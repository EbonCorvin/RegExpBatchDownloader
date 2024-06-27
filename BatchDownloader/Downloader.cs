using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace BatchDownloader
{
    class Downloader
    {
        private const String USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
        private class DownloadThreadParameters
        {
            public int ThreadId { get; set; }
            public IEnumerable<String> UrlList { get; set; }
        }

        private List<Regex> regexs= new List<Regex>();
        private String outputFolder;

        private int fileNameCount = 0;

        private int threadId = 0;

        private ConcurrentDictionary<int, String> threadStatus = new ConcurrentDictionary<int, String>();
        private ConcurrentDictionary<int, int> removeFromStatus = new ConcurrentDictionary<int, int>();
        private int activeThread = 0;
        private int errorCount = 0;
        Stopwatch stopwatch;

        private List<string> urlList;

        public Downloader(String firstPage, List<Regex> regexList, String output) : this(new List<string>() { firstPage }, output)
        {
            regexs = regexList;
        }

        public Downloader(List<string> urlList, String output)
        {
            outputFolder = output;
            Directory.CreateDirectory(outputFolder);
            this.urlList = urlList;
        }
        
        public void Start()
        {
            fetchPage(urlList, 0);
            Thread.Sleep(1000);
            stopwatch = Stopwatch.StartNew();
            while(activeThread > 0)
            {
                StatusUpdate();
                Thread.Sleep(1000);
            }
            stopwatch.Stop();
            Console.Clear();
            Console.WriteLine("Process finished in {0:0} seconds", stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine("Total {0} error(s)", errorCount);
            if(errorCount > 0)
            {
                Console.WriteLine("Check the generated text file for detail");
            }
            
        }

        private bool isLastDepth(int depth)
        {
            return depth == regexs.Count;
        }

        private void downloadAsFile(String url)
        {
            String fileName = outputFolder + "\\" + url.Substring(url.LastIndexOf("/") + 1);
            if (File.Exists(fileName))
            {
                fileName = outputFolder + "\\" + (fileNameCount++) + url.Substring(url.LastIndexOf("/") + 1);
            }
            FileStream file = File.Create(fileName, 8192);
            byte[] buffer = new byte[8192];
            int readCount = 0;
            WebClient reqClient = new WebClient();
            reqClient.Headers["User-Agent"] = USER_AGENT;
            Stream respStream = reqClient.OpenRead(url);
            while ((readCount = respStream.Read(buffer, 0, 8192)) != 0)
            {
                file.Write(buffer, 0, readCount);
            }
            file.Close();
            respStream.Close();
        }

        private String download(String url, int regexIdx)
        {
            WebClient reqClient = new WebClient();
            reqClient.Headers["User-Agent"] = USER_AGENT;
            reqClient.Headers["Accept"] = "*/*";
            bool lastDepth = isLastDepth(regexIdx);
            String content = "";
            try
            {
                Stream respStream = reqClient.OpenRead(url);
                StreamReader sr = new StreamReader(respStream);
                String pageContent = sr.ReadToEnd();
                respStream.Close();
                content = pageContent;
            }
            catch (WebException ex)
            {
                errorCount++;
                String errorResponse = "";
                Stream response = ex.Response.GetResponseStream();
                if (response != null)
                {
                    errorResponse = new StreamReader(response).ReadToEnd();
                }
                File.WriteAllText(regexIdx + "_downloaderr_" + Regex.Replace(url, "[\\/\\?\\-\\:\\#]", "_") + ".txt", url + Environment.NewLine + ex.ToString() + Environment.NewLine + errorResponse);
                response.Close();
            }
            catch (Exception ex)
            {
                errorCount++;
                File.WriteAllText(regexIdx + "_downloaderr_" + Regex.Replace(url, "[\\/\\?\\-\\:\\#]", "_") + ".txt", url + Environment.NewLine + ex.ToString() + Environment.NewLine);

            }
            return content;
        }

        private IEnumerable<String> extractNextUrl(String pageContent, int regexIdx, String url, int threadId)
        {
            if(pageContent=="")
                return new String[] { };
            List<String> subList = new List<string>();
            MatchCollection matches = regexs[regexIdx].Matches(pageContent);
            if (matches.Count == 0)
            {
                errorCount++;
                File.WriteAllText(regexIdx + "_nomatch_" + Regex.Replace(url, "[\\/\\?\\-\\:\\#]", "_") + ".txt", url + Environment.NewLine + pageContent);
                return new String[] { };
            }

            foreach (Match match in matches)
            {
                String subUrl = match.Groups[1].Value;
                if (!subUrl.StartsWith("http"))
                {
                    if (subUrl.StartsWith("/"))
                    {
                        subUrl = url.Substring(0, url.IndexOf("/", 9)) + subUrl;
                    }
                    else
                    {
                        if (subUrl.StartsWith("."))
                            subUrl = subUrl.Substring(1);
                        subUrl = url.Substring(0, url.LastIndexOf('/')) + subUrl;
                    }
                }
                subList.Add(WebUtility.HtmlDecode(subUrl));
            }
            return subList;
        }

        private void fetchPage(IEnumerable<String> urlList, int regexIdx)
        {
            for (int i = 0; i < 4; i++)
            {
                var newList = urlList.Where(new Func<string, int, bool>((url, idx) => idx % 4 == i));
                if (newList.Count() == 0)
                    continue;

                var threadParams = new DownloadThreadParameters()
                {
                    ThreadId = threadId++,
                    UrlList = newList.ToArray()
                };
                new Thread(new ParameterizedThreadStart((param) =>
                {
                    activeThread++;
                    DownloadThreadParameters parameters = (DownloadThreadParameters)param;
                    int threadNo = parameters.ThreadId ;
                    var urls = parameters.UrlList;
                    int total = urls.Count();
                    int count = 0;
                    if (!isLastDepth(regexIdx))
                    {

                        List<String> pageContent = new List<string>();
                        foreach(String url in urls)
                        {
                            threadStatus[threadNo] = String.Format("Depth {0:D2} - Downloading page {1} / {2}", regexIdx, ++count, total);
                            pageContent.Add(download(url, regexIdx));
                        }
                        List<String> nextUrls = new List<String>();
                        String firstUrl = urls.First();
                        count = 0;
                        foreach (String content in pageContent)
                        {
                            threadStatus[threadNo] = String.Format("Depth {0:D2} - Parsing page {1} / {2}", regexIdx, ++count, total);
                            var nextUrl = extractNextUrl(content, regexIdx, firstUrl, threadNo);
                            nextUrls.AddRange(nextUrl);
                        }
                        fetchPage(nextUrls, regexIdx + 1);

                        /*foreach (String url in urls)
                        {
                            String pageContent = download(url, regexIdx);
                            var nextUrls = extractNextUrl(pageContent, regexIdx, url, threadNo);
                            fetchPage(nextUrls, regexIdx + 1);
                        }*/
                    }
                    else
                    {
                        foreach (var item in urls)
                        {
                            threadStatus[threadNo] = String.Format("Depth {0:D2} - Downloading files {1} / {2}", regexIdx, ++count, total);
                            downloadAsFile(item);
                        }
                    }
                    threadStatus[threadNo] = String.Format("Depth {0:D2} - Done", regexIdx);
                    removeFromStatus[threadNo] = 3;
                    activeThread--;
                })).Start(threadParams);
            }
        }
    
        private void StatusUpdate()
        {
            Console.Clear();
            foreach(var item in threadStatus)
            {
                Console.WriteLine("Thread {0:D2} - {1}", item.Key, item.Value);
                if (removeFromStatus.ContainsKey(item.Key))
                {
                    if (--removeFromStatus[item.Key] == 0)
                    {
                        String str = "";
                        threadStatus.TryRemove(item.Key, out str);
                    }
                }
            }
            Console.WriteLine("Elapsed time: {0:0} seconds", stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine("Error: {0}", errorCount);
        }
    }
}
