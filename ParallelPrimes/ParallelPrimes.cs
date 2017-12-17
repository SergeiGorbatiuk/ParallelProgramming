﻿using System;
using System.Collections;
using System.Collections.Generic;
 using System.Diagnostics;
 using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelPrimes
{
    class ParallelPrimes
    {
        static void Main(string[] args)
        {
            int range = 1000000;
            Console.WriteLine("Starting Thread manner with {0} range length", range );
            Stopwatch timer = Stopwatch.StartNew();
            Thread_manner(range, 8);
            timer.Stop();
            Console.Write("Thread manner DONE. ");
            Console.WriteLine("Time elapsed: {0}", timer.Elapsed);
            
            Console.WriteLine("Starting Task manner with {0} range length", range );
            timer.Restart();
            Task_manner(1, range + 1);
            timer.Stop();
            Console.Write("Task manner DONE. ");
            Console.WriteLine("Time elapsed: {0}", timer.Elapsed);
            
            Console.WriteLine("Starting Task manner with {0} range length", range );
            timer.Restart();
            ThreadPool_manner(range);
            timer.Stop();
            Console.Write("Task manner DONE. ");
            Console.WriteLine("Time elapsed: {0}", timer.Elapsed);
            
        }

        //////////////////////////////////////////////////////////////
        private static bool isPrime(int n)
        {

            if (n % 2 == 0 && n != 2 || n == 1) return false;
            for (int i = 3; i <= Math.Round(Math.Sqrt(n)); i = i+2)
            {
                if (n % i == 0) return false;
            }
            return true;
        }

        private static void partCheck(int[] table, int start, int finish)
        {
            for (int i = start; i < finish; i++)
            {
                if (!isPrime(i)) table[i] = 1;
            }
        }

        public static List<int> Thread_manner(int range, int thCount)
        {
            int[] table = new int[range];
            int length = table.Length;
            int threadCount = thCount;
            Thread[] threads = new Thread[threadCount];
            int prev = 0;
            int cur = 0;
            int divisor = 1;
            for (int i = 0; i < threadCount-1; i++)
            {
                divisor *= 2;
                cur = length - length / divisor;
                int cur1 = cur;
                int prev1 = prev;
                threads[i] = new Thread(() => partCheck(table, prev1, cur1));
                threads[i].Start();
                prev = cur;
            }
            threads[threads.Length-1] = new Thread(() => partCheck(table, cur, table.Length));
            threads[threads.Length-1].Start();
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
            List<int> result = new List<int>();
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i] == 0)
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public static List<int> Task_manner(int start, int finish)
        {
            List<int> result = new List<int>();
            if (finish - start >= 400)
            {
                int median = (finish - start) / 2;
                Task<List<int>> leftTask = Task.Run(() => checkRange(start, median));
                Task<List<int>> rightTask = Task.Run(() => checkRange(median, finish));

                Task.WaitAll(leftTask, rightTask);
                result.AddRange(leftTask.Result);
                result.AddRange(rightTask.Result);
            }
            else
            {
                result.AddRange(checkRange(start, finish));
            }
            
            return result;
        }

        private static List<int> checkRange(int start, int finish)
        {
            List<int> result = new List<int>();
            for (int i = start; i < finish; i++)
            {
                if (isPrime(i))
                {
                    result.Add(i);
                }
            }
            return result;
        }

        public static List<int> ThreadPool_manner(int range)
        {
            List<int> result = new List<int>();
            int[] table = new int[range];
            var resetEvent = new ManualResetEvent(false);
            Wrapper wrapper = new Wrapper(resetEvent);
            ThreadPool.QueueUserWorkItem(wrapper.checkRangeCallback, table);
            resetEvent.WaitOne();
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i] == 1)
                {
                    result.Add(i);
                }
            }
            return result;
        }

    }

    class Wrapper
    {
        private ManualResetEvent _done;

        public Wrapper(ManualResetEvent done)
        {
            this._done = done;
        }
        public void checkRangeCallback(Object obj)
        {
            int[] range = (int[]) obj;
            for (int i = 0; i < range.Length; i++)
            {
                if (isPrime(i))
                {
                    range[i] = 1;
                }
            }
            _done.Set();
        }
        private static bool isPrime(int n)
        {

            if (n % 2 == 0 && n != 2 || n == 1) return false;
            for (int i = 3; i <= Math.Round(Math.Sqrt(n)); i = i+2)
            {
                if (n % i == 0) return false;
            }
            return true;
        }
    }
}