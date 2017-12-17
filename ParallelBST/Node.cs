using System;

namespace ParallelBST
{
    public class Node<T> where T : IComparable<T>    
    {
        internal T key;
        internal volatile int locked = 0;
        internal bool deleted = false;

        internal Node<T> rightChild = null;
        internal Node<T> leftChild = null;

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