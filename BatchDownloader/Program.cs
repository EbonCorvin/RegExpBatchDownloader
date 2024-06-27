using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace BatchDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("This tool accept one parameter, which is the path to the configuration file");
                Console.WriteLine("The format of the configuration file:");
                Console.WriteLine("1st line: The url of the first page");
                Console.WriteLine("2nt line: The file system path for saving the downloaded file");
                Console.WriteLine("3rd line and so on: The Regular Expresses for each depth in the site");
                return;
            }
            StreamReader config = File.OpenText(args[0]);
            String startUrl = config.ReadLine();
            bool isAllUrlMode = (startUrl == "all_url_mode");

            String outputFolder = config.ReadLine();
            if (isAllUrlMode)
            {
                List<String> urlList = new List<String>();
                String url = null;
                while ((url = config.ReadLine()) != null)
                {
                    urlList.Add(url);
                }
                new Downloader(urlList, outputFolder);
                Console.Read();
                return;
            }

            List<Regex> regexList = new List<Regex>();
            String line=null;
            while ((line = config.ReadLine()) != null)
            {
                regexList.Add(new Regex(line, RegexOptions.Singleline | RegexOptions.Compiled));
                //Console.WriteLine(regexList.Last().ToString());
            }
            config.Close(); 
            new Downloader(startUrl, regexList, outputFolder).Start();
            Console.Read();
        }
    }
}
