using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelProgramming
{
    internal class ParallelHasher
    {
        public static void Main(string[] args)
        {
            ParallelFolderHasher pfh = new ParallelFolderHasher();
            var res = pfh.HashFolder("/home/sergey/sergey/DataScience");
            Console.WriteLine(res);
        }
        
    }

    class ParallelFolderHasher
    {
        private MD5 md5Hash;
        
        public ParallelFolderHasher()
        {
            md5Hash = MD5.Create();
        }
        
        public string HashFolder(string path)
        {
            StringBuilder raw_resutl = new StringBuilder();
            string[] files = Directory.GetFiles(path);
            /* get hash of all files in current folder*/
            var fileTasksCount = files.Length == 0 ? 0 : files.Length / 100 + 1;
            if (fileTasksCount != 0)
            {
                int[] parts_indices = new int[fileTasksCount + 1]; // array of indicies including begin and end
                parts_indices[0] = 0;
                for (int i = 1; i < parts_indices.Length-1; i++)
                {
                    parts_indices[i] = files.Length / fileTasksCount * i;
                }
                parts_indices[parts_indices.Length - 1] = files.Length;
                List<Task<string>> fileTasks = new List<Task<string>>();
                for (int i = 0; i < fileTasksCount; i++)
                {
                    var j = i;
                    Task<string> tsk = new Task<string>(() => HashFileSet(files, parts_indices[j], parts_indices[j+1]));
                    tsk.Start();
                    fileTasks.Add(tsk);
                }
                Task.WaitAll(fileTasks.ToArray());
                foreach (var fileTask in fileTasks.ToArray())
                {
                    raw_resutl.Append(fileTask.Result);
                }
            }
            
            /*get hash of nested folders*/
            string[] folders = Directory.GetDirectories(path);
            var folderTasksCount = folders.Length == 0 ? 0 : folders.Length / 10 + 1;
            if (folderTasksCount != 0)
            {
                int[] parts_indices = new int[folderTasksCount + 1]; // array of indicies including begin and end
                parts_indices[0] = 0;
                for (int i = 1; i < parts_indices.Length-1; i++)
                {
                    parts_indices[i] = folders.Length / folderTasksCount * i;
                }
                parts_indices[parts_indices.Length - 1] = folders.Length;
                List<Task<string>> folderTasks = new List<Task<string>>();
                for (int i = 0; i < folderTasksCount; i++)
                {
                    var j = i;
                    Task<string> tsk = new Task<string>(() => HashFolderSet(folders, parts_indices[j], parts_indices[j+1]));
                    tsk.Start();
                    folderTasks.Add(tsk);
                }
                Task.WaitAll(folderTasks.ToArray());
                foreach (var folderTask in folderTasks.ToArray())
                {
                    raw_resutl.Append(folderTask.Result);
                }
            }
            raw_resutl.Append(path.Split('/').Last());
            
            return GetMd5StringHash(raw_resutl.ToString());

        }

        private string HashFolderSet(string[] folders, int start, int finish)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = start; i < finish; i++)
            {
                var j = i;
                stringBuilder.Append(HashFolder(folders[j]));
            }
            return stringBuilder.ToString();
        }
        
        private string GetMd5StringHash(string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public string HashFileSet(string[] files, int start, int stop)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = start; i < stop; i++)
            {
                stringBuilder.Append(GetMd5FileHash(files[i]));
            }
            return stringBuilder.ToString();
        }
        
        private string GetMd5FileHash(string path)
        {
            var filestream = File.OpenRead(path);
            var hash = md5Hash.ComputeHash(filestream);
            StringBuilder s = new StringBuilder();
            foreach (var b in hash)
            {
                s.Append(b.ToString("x2"));
            }
            var filename = path.Split('/').Last();
            return GetMd5StringHash(filename + s.ToString());
        }
    }
}
