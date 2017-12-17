using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParallelLinkAnalyzer
{
    internal class ParallelLinks
    {
        public static void Main(string[] args)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ParallelDownloader pd = new ParallelDownloader();
            var RESULT = pd.GoInspectAsync("http://www.bridgeclub.ru/", 1).Result;
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed.Milliseconds);// weird result
            /* test it by setting a stop mark and running in debug mode. Watch RESULT in variables*/
            Console.WriteLine("Done");
        }
    }
    
    public class ParallelDownloader
    {
        private Task<InspectResult> InspectPageAsync(string url)
        {
            return Task.Run(() =>
            {
                InspectResult iRes = new InspectResult {selfLink = url};
                string hrefPattern = "href\\s*=\\s*(?:[\"'])(http[^\"']*|[^\"']*\\.html?|[^\"']*\\.php\\??)(?:[\"'])";
                WebClient client = new WebClient();
                try
                {
                    string source = client.DownloadString(url);
                    Match m = Regex.Match(source, hrefPattern, 
                        RegexOptions.IgnoreCase | RegexOptions.Compiled, 
                        TimeSpan.FromSeconds(1));
                    while (m.Success)
                    {
                        var link = m.Groups[1].Value;
                        if (! link.StartsWith("http"))
                        {
                            link = url.Split('?')[0] + link;
                        }
                        iRes.links.Add(link);
                        m = m.NextMatch();
                    }
                    iRes.length = source.Length;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unrezolvable path occured");
                }
                return iRes;
            });
        }

        public async Task<InspectResult> GoInspectAsync(string url, int depth)
        {
            List<Task<InspectResult>> runningTasks = new List<Task<InspectResult>>();
            InspectResult iRes = await InspectPageAsync(url);
            if (depth > 0)
            {
                foreach (var link in iRes.links)
                {
                    runningTasks.Add(GoInspectAsync(link, depth-1));
                }
                foreach (var runningTask in runningTasks)
                {
                    iRes.children.Add(await runningTask);
                }
            }
            return iRes;
        }
        
    }

    public class InspectResult
    {
        public string selfLink;
        public List<string> links = new List<string>();
        public List<InspectResult> children = new List<InspectResult>();
        public int length;
    }
}