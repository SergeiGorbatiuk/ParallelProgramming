using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Xsl.Runtime;

namespace ParallelBST
{
    public class ParallelBST<T> : IEnumerable<Node<T>> where T : IComparable<T>
    {
        private Node<T> root = null;
        private Hashtable buffer = new Hashtable();
        private const int maxDelayed = 5;
        private object _lockBuffer = new object();
        private object _lockRoot = new object();
        
        public Task Insert(T key)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_lockBuffer)
                {
                    if (buffer.ContainsKey(key))
                    {
                        if (buffer[key].Equals(1))
                        {
                            Console.WriteLine("Eliminating " + key + " from buffer");
                            buffer.Remove(key);    
                        }
                    }
                    else
                    {
                        buffer.Add(key, 0);
                    }
                    if (buffer.Count >= maxDelayed)
                    {
                        Console.WriteLine("Max buffer, performiang operations");
                        PerformOperations().Wait();
                    }
                }
            });
        }

        public Task Delete(T key)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_lockBuffer)
                {
                    if (buffer.ContainsKey(key))
                    {
                        if (buffer[key].Equals(0))
                        {
                            Console.WriteLine("Eliminating " + key + " from buffer");
                            buffer.Remove(key);    
                        }
                    }
                    else
                    {
                        buffer.Add(key, 1);
                    }
                }
                if (buffer.Count >= maxDelayed)
                {
                    Console.WriteLine("Max buffer, performiang operations");
                    PerformOperations().Wait();
                }
            });
        }

        public Task<bool> Search(T key)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (_lockBuffer)
                {
                    if (buffer.Contains(key))
                    {
                        Console.WriteLine("Node found" + key);
                        return true;
                    }
                }
                var result = SearchTree(key);
                return result != null;
            });
            
        }

        public Task ForcePerform()
        {
            return PerformOperations();
        }

        private Task PerformOperations()
        {
            return Task.Factory.StartNew(() =>
            {
                Hashtable tempbuff = (Hashtable) buffer.Clone();
                buffer.Clear();
                    
                Dictionary<T, Task<bool>> runningOperations = new Dictionary<T, Task<bool>>();
                foreach (var cur in tempbuff)
                {
                    var taskToBeDone = cur.Value.Equals(0) ? PerformInsert((T) cur.Key) : PerformDelete((T) cur.Key);
                    runningOperations.Add((T)cur.Key, taskToBeDone);
                    
                    /*if (cur.Value.Equals(0))
                    {
                        runningOperations.Add((T)cur.Key, PerformInsert((T)cur.Key));
                    }
                    else
                    {
                        runningOperations.Add((T)cur.Key, PerformDelete((T)cur.Key));
                    }*/
                }
                
                foreach (var runningOperation in runningOperations)
                {
                    var res = runningOperation.Value.Result;
                    while (!res)
                    {
                        Console.WriteLine("Repeating operation");
                        res = PerformInsert(runningOperation.Key).Result;
                    }
                }
            });  
        }

        private Node<T> SearchTree(T key)
        {
            Console.WriteLine("Searching " + key + " in tree");
            
            if (root == null) 
                return null;
            
            var current = root;
            while (true)
            {
                if (current.deleted)
                {
                    current = root;
                    continue;
                }
                if (current.key.CompareTo(key) > 0)
                {
                    if (current.leftChild != null)
                    {
                        current = current.leftChild;
                    }
                    else
                    {
                        Console.WriteLine("Node not found " + key);
                        return null;
                    }
                    continue;    
                }
                else if (current.key.CompareTo(key) < 0)
                {
                    if (current.rightChild != null)
                    {
                        current = current.rightChild;
                    }
                    else
                    {
                        Console.WriteLine("Node not found " + key);
                        return null;
                    }
                    continue;
                }
                Console.WriteLine("Node found " + key);
                return current;
            }
        }
        
        private Task<bool> PerformInsert(T key)
        {
            return Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Performing insertion " + key);
                
                var insertingNode = new Node<T>(key);
                if (root == null)
                {
                    lock (_lockRoot) // may look stupid but this actually makes sense
                    {
                        if (root == null)
                        {
                            root = insertingNode;
                            Console.WriteLine("Succesfully inserted root " + key);
                            return true;
                        }
                        Console.WriteLine("Fault inserting root");
                        return false;
                    }    
                }
                var current = root;
                while (true)
                {
                    if (Interlocked.CompareExchange(ref current.locked, 1, 0) == 0)
                    {
                        if (current.deleted)
                        {
                            current = root;
                            continue;
                        }
                        if (current.key.CompareTo(key) < 0)
                        {
                            if (current.rightChild != null)
                            {
                                current.locked = 0;
                                current = current.rightChild;
                            }
                            else
                            {
                                current.rightChild = insertingNode;
                                current.locked = 0;
                                Console.WriteLine("Inserted " + key);
                                return true;
                            }  
                            continue;
                        }
                        else if (current.key.CompareTo(key) > 0)
                        {
                            if (current.leftChild != null)
                            {
                                current.locked = 0;
                                current = current.leftChild;
                            }
                            else
                            {
                                current.leftChild = insertingNode;
                                current.locked = 0;
                                Console.WriteLine("Inserted " + key);
                                return true;
                            }
                            continue;
                        }
                        current.locked = 0;
                        return true;
                    }
                }
            });
        }

        private Task<bool> PerformDelete(T key)
        {
            return Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Start deleting " + key);
                /* need to perform special search remembering and locking parent */
                if (root == null) return true;
                Node<T> targetParent = null;
                var targetNode = root;
                var isLeftChild = true;
                while (true)
                {
                    if (Interlocked.CompareExchange(ref root.locked, 1, 0) == 0)
                    {
                        if (root.key.CompareTo(key) < 0)
                        {
                            if (root.rightChild == null)
                            {
                                root.locked = 0;
                                return true;
                            }
                            targetNode = root.rightChild;
                            isLeftChild = false;
                            targetParent = root;
                            break; // now targetParent (== root) is still locked
                        }
                        if (root.key.CompareTo(key) > 0)
                        {
                            if (root.leftChild == null)
                            {
                                root.locked = 0;
                                return true;
                            }
                            targetNode = root.leftChild;
                            isLeftChild = true;
                            targetParent = root;
                            break; // now targetParent (== root) is still locked
                        }
                        /* if here, then root has to be deleted */
                        break; //and it's still locked
                    }
                }
                Console.WriteLine("Found node to delete " + key);
                if (!targetNode.key.Equals(key)) //then not the root has to be deleted. targetParent != null
                {
                    while (true)
                    {
                        if (Interlocked.CompareExchange(ref targetNode.locked, 1, 0) == 0)
                        {
                            if (targetNode.deleted)
                            {
                                return false; // bottleneck
                            }
                            /* now locking two nodes */
                            if (targetNode.key.CompareTo(key) < 0)
                            {
                                if (targetNode.rightChild == null)
                                {
                                    targetNode.locked = 0;
                                    targetParent.locked = 0;
                                    return true;
                                }
                                targetParent.locked = 0;
                                targetParent = targetNode;
                                isLeftChild = false;
                                targetNode = targetNode.rightChild;
                            }
                            else if (targetNode.key.CompareTo(key) > 0)
                            {
                                if (targetNode.leftChild == null)
                                {
                                    targetNode.locked = 0;
                                    targetParent.locked = 0;
                                    return true;
                                }
                                targetParent.locked = 0;
                                targetParent = targetNode;
                                isLeftChild = true;
                                targetNode = targetNode.leftChild;
                            }
                            break; //still locking two nodes
                        }
                    }    
                }
                
                /* now we can try to perform deletion, holding targetNode and it's parent */
                if (targetNode.leftChild != null && targetNode.rightChild != null) // both children
                {
                    var subst = targetNode.rightChild;
                    var substParent = targetNode;
                    while (true)
                    {
                        if (Interlocked.CompareExchange(ref subst.locked, 1, 0) == 0)
                        {
                            if (subst.deleted)
                            {
                                subst = targetNode.rightChild;
                                continue;
                            }
                            if (subst.leftChild != null)
                            {
                                substParent = subst;
                                subst = subst.leftChild;
                                if (substParent != targetNode)
                                {
                                    substParent.locked = 0;
                                }
                            }
                            else break; //subst and it's parent still locked!
                        }
                    }
                    /* now performing substitution */
                    if (targetParent == null) // then we r deleting root
                    {
                        subst.leftChild = root.leftChild;
                        if (targetNode.rightChild != subst)
                        {
                            substParent.leftChild = null;
                            subst.rightChild = root.rightChild;
                        }
                        root = subst;
                        targetNode.deleted = true;

                        subst.locked = 0;
                        substParent.locked = 0;
                        targetNode.locked = 0;
                        return true;
                    }
                    
                    if (targetNode.rightChild != subst)
                    {
                        substParent.leftChild = null;
                        subst.rightChild = targetNode.rightChild;
                    }
                    if (isLeftChild)
                    {
                        targetParent.leftChild = subst;
                    }
                    else
                    {
                        targetParent.rightChild = subst;
                    }
                    subst.leftChild = targetNode.leftChild;
                    targetNode.deleted = true;
                    subst.locked = 0;
                    substParent.locked = 0;
                    targetNode.locked = 0;
                    targetParent.locked = 0;
                    return true;
                }

                if (targetNode.leftChild == null && targetNode.rightChild == null) // no children
                {
                    if (isLeftChild)
                    {
                        targetParent.leftChild = null;
                        targetNode.deleted = true;
                    }
                    else
                    {
                        targetParent.rightChild = null;
                        targetNode.deleted = true;
                    }
                    targetNode.locked = 0;
                    targetParent.locked = 0;
                    return true;
                }

                if (targetNode.leftChild == null) //only right child
                {
                    targetParent.rightChild = targetNode.rightChild;
                    targetNode.deleted = true;
                    targetNode.locked = 0;
                    targetParent.locked = 0;
                    return true;
                }
                
                // only left child
                targetParent.leftChild = targetNode.leftChild;
                targetNode.deleted = true;
                targetNode.locked = 0;
                targetParent.locked = 0;
                return true;
            });
        }
        
        public void Print()
        {
            var height = GetHeight(root);
            List<int> coords = new List<int>();
            coords.Add((int) Math.Pow(2.0, height));
            coords.Add(-1);

            var counter = 1;
            var line = 0;

            foreach (var node in this)
            {
                if (coords[0] == -1)
                {
                    Console.WriteLine();
                    line++;
                    coords.RemoveAt(0);
                    counter = 1;
                    coords.Add(-1);
                }
                while (coords[0] != counter)
                {
                    Console.Write(" ");
                    counter++;
                }
                Console.Write(node);
                coords.RemoveAt(0);
                if (node.leftChild != null)
                {
                    coords.Add(counter - (int)Math.Pow(2.0, height - line -1));
                }
                if (node.rightChild != null)
                {
                    coords.Add(counter + (int)Math.Pow(2.0, height - line -1));
                }
                counter++;
            }
            Console.WriteLine();
        }

        int GetHeight(Node<T> aNode) {
            if (aNode == null) {
                return -1;
            }

            int lefth = GetHeight(aNode.leftChild);
            int righth = GetHeight(aNode.rightChild);

            if (lefth > righth) {
                return lefth + 1;
            } else {
                return righth + 1;
            }
        }
        
        
        public IEnumerator<Node<T>> GetEnumerator()
        {
            List<Node<T>> queue = new List<Node<T>>();
            if (root != null)
            {
                queue.Add(root);
            }
            while (queue.Count != 0)
            {
                var node = queue[0];
                queue.RemoveAt(0);
                if (node.leftChild != null)
                {
                    queue.Add(node.leftChild);
                }
                if (node.rightChild != null)
                {
                    queue.Add(node.rightChild);
                }
                yield return node;
            }
            yield break;
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}