using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Pinger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var url = args.GetArg("--urls");
            IEnumerable<string> urls = new List<string>();
            if (url is null)
            {
                var urlSource = args.GetArg("--urls-src");
                if (urlSource is null)
                {
                    Console.WriteLine("expect one of --urls or --urls-src args");
                    return;
                }

                if (!urlSource.EndsWith(".txt") || !File.Exists(urlSource))
                {
                    Console.WriteLine($"{urlSource} file does not exist or is not valid .txt file");
                }

                urls = GetUrlsFromFile(urlSource);
            }
            else
            {
                urls = args.GetUrls("--url", new[] { "--urls-src", "--repeat", "--o-format", "--o-name" });
            }

            var repeat = args.GetArg("--repeat");
            var outFormat = args.GetArg("--o-format");
            var outFile = args.GetArg("--o-name");
            var repeatValue = 1;
            if (repeat is not null && int.TryParse(repeat, out var r))
            {
                repeatValue = r;
                return;
            }

            if (outFormat is null)
            {
                if (outFile is not null)
                {
                    Console.WriteLine("both --o-format and --o-name should be provided");
                    return;
                }
            }

            foreach (var u in urls)
            {
                var time = Ping(u, repeatValue);
                if (outFormat is null) continue;
                if (outFile is null)
                {
                    //WriteToFile(outFormat);
                    continue;
                }

                //WriteToFile(outFormat, outFile);
            }
        }

        static float Ping(string url, int repeat = 1)
        {
            var ping = new Ping();
            var total = 0L;
            for (var i = 0; i < repeat; i++)
            {
                var reply = ping.Send(url);
                if (reply is { Status: IPStatus.Success })
                {
                    Console.WriteLine($"reply from: {reply.Address}");
                    Console.WriteLine($"round trip times in milli-seconds: {reply.RoundtripTime}");
                    total += reply.RoundtripTime;
                }

                if (reply != null && reply.Status != IPStatus.Success)
                {
                    Console.WriteLine("cannot be reached");
                }
            }

            return (float)total / repeat;
        }

        static string[] GetUrlsFromFile(string path)
        {
            var urls = new List<string>();

            using var sr = new StreamReader(path);
            string url;
            while ((url = sr.ReadLine()) is not null)
            {
                urls.Add(url);
            }

            return urls.ToArray();
        }

        static void WriteToFile(string url, float time, string format, string name = "output")
        {
            switch (format)
            {
                case "xml":
                    WriteToXml(url, time, name + ".xml");
                    break;
                case "txt":
                    WriteToTxt(url, time, name + ".txt");
                    break;
                case "html":
                    WriteToHtml(url, time, name + ".html");
                    break;
            }
        }

        static void WriteToTxt(string text, float time, string path)
        {
            var file = File.Open(path, FileMode.Append, FileAccess.Write);
            using var sw = new StreamWriter(file);

            sw.WriteLine("time:");
        }

        static void WriteToXml(string text, float time, string path)
        {
        }

        static void WriteToHtml(string text, float time, string path)
        {
        }
    }

    static class ArgsReader
    {
        public static string GetArg(this string[] args, string argName)
        {
            var indexOfArgName = Array.IndexOf(args, argName);

            if (indexOfArgName == -1 || indexOfArgName == args.Length)
            {
                return null;
            }

            return args[indexOfArgName + 1];
        }

        public static IEnumerable<string> GetUrls(this string[] args, string urlsArgsName, string[] argNames)
        {
            var indexOfArgName = Array.IndexOf(args, urlsArgsName);

            if (indexOfArgName == -1 || indexOfArgName == args.Length)
            {
                return Array.Empty<string>();
            }

            var urls = new List<string>();
            var c = indexOfArgName + 1;
            while (!argNames.Contains(args[indexOfArgName + c]) && indexOfArgName + c < args.Length)
            {
                urls.Add(args[indexOfArgName + c]);
            }

            return urls;
        }

        public static bool HasFlag(this string[] args, string flag)
        {
            return args.Contains(flag);
        }
    }
}