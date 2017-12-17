using System;

namespace ParallelBST
{
    public class Node<T> where T : IComparable<T>    
    {
        public T key;
        public volatile int locked = 0;
        public bool deleted = false;

        public Node<T> rightChild = null;
        public Node<T> leftChild = null;

        public Node(T key)
        {
            this.key = key;
        }
        
        public override string ToString()
        {
            return key.ToString();
        }
    }
}