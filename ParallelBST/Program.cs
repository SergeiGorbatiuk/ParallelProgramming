using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelBST
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ParallelBST<int> tree = new ParallelBST<int>();
            var tasks = new List<Task>();
            tasks.Add(tree.Insert(0));
            tasks.Add(tree.Insert(1));
            tasks.Add(tree.Insert(2));
            tasks.Add(tree.Insert(4));
            tasks.Add(tree.Insert(3));
            tasks.Add(tree.Insert(7));
            tasks.Add(tree.Insert(12));
            tasks.Add(tree.Delete(1));
            tasks.Add(tree.Search(3));
            tasks.Add(tree.ForcePerform());
            Task.WaitAll(tasks.ToArray());
            tree.Print();
            tasks.Clear();
            tasks.Add(tree.Search(0));
            tasks.Add(tree.Search(3));
            tasks.Add(tree.Search(11));
            Task.WaitAll(tasks.ToArray());
            Console.WriteLine();
            Console.WriteLine("DONE");
        }
    }
}