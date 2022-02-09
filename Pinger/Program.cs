using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Pinger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (!args.Any() || args.Contains("--help"))
            {
                Console.WriteLine(PingArgs.UrlsArg + " | " + PingArgs.UrlsFileArg);
                Console.WriteLine(PingArgs.AttemptsArg);
                Console.WriteLine(PingArgs.DelayArg);
                Console.WriteLine(PingArgs.FormatArgName + " console | txt | xml | html");
                Console.WriteLine(PingArgs.OutputArgName + " default name: output");
                return;
            }

            try
            {
                await Run(new PingArgs(args));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static async Task Run(PingArgs args)
        {
            await Task.Delay(args.Delay);
            var replies = args.Urls.ToDictionary(u => u, u => Ping(u, args.Timeout, args.Attempts));

            if ((args.Formats & OutputFormats.Console) != 0)
            {
                Console.WriteLine($"avg roundtrip time: {CalculateAvgRoundtripTime(replies)}");
                foreach (var reply in replies)
                {
                    Console.WriteLine();
                    if (reply.Value.Any(r => r.RoundtripTime < 0))
                    {
                        var sum = reply.Value.Select(r => r.RoundtripTime).Count(time => time < 0);
                        Console.WriteLine($"address: {reply.Key} (cannot reach {sum} times)");
                    }
                    else
                    {
                        Console.WriteLine($"address: {reply.Key}");
                    }

                    Console.WriteLine(
                        $"roundtrip time: {CalculateAvgRoundtripTime(reply.Value)}");
                }
            }

            if ((args.Formats & OutputFormats.Txt) != 0)
            {
                WriteToTxt(replies, args.OutputFileName);
            }

            if ((args.Formats & OutputFormats.Html) != 0)
            {
                WriteToHtml(replies, args.OutputFileName);
            }

            if ((args.Formats & OutputFormats.Xml) != 0)
            {
                WriteToXml(replies, args.OutputFileName);
            }
        }

        static double CalculateAvgRoundtripTime(Dictionary<string, List<PingResponse>> replies)
        {
            var total = 0D;
            var count = 0;
            foreach (var reply in replies)
            {
                var result = CalculateAvgRoundtripTime(reply.Value);
                if (!double.IsNaN(result))
                {
                    total += result;
                    count++;
                }
            }

            return total / count;
        }

        static double CalculateAvgRoundtripTime(List<PingResponse> replies)
        {
            return replies.Where(r => r.RoundtripTime >= 0).Sum(r => r.RoundtripTime) /
                   (double) replies.Count(r => r.RoundtripTime >= 0);
        }

        static List<PingResponse> Ping(string url, int timeout, int attempts = 1)
        {
            if (attempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(attempts));
            }

            var ping = new Ping();
            var responses = new List<PingResponse>();
            for (var i = 0; i < attempts; i++)
            {
                PingResponse response;
                try
                {
                    var reply = ping.Send(url, timeout);

                    if (reply?.Status == IPStatus.Success)
                    {
                        response = new PingResponse()
                        {
                            Address = reply.Address.ToString(),
                            RoundtripTime = reply.RoundtripTime
                        };
                    }
                    else
                    {
                        response = new PingResponse()
                        {
                            Address = url,
                            RoundtripTime = -1
                        };
                    }
                }
                catch (Exception e)
                {
                    response = new PingResponse()
                    {
                        Address = url,
                        RoundtripTime = -1
                    };
                }

                responses.Add(response);
            }

            return responses;
        }

        static void WriteToTxt(Dictionary<string, List<PingResponse>> replies, string path)
        {
            using var sw = new StreamWriter(path + ".txt");

            sw.WriteLine($"avg roundtrip time: {CalculateAvgRoundtripTime(replies)}");
            foreach (var reply in replies)
            {
                sw.WriteLine();
                if (reply.Value.Any(r => r.RoundtripTime < 0))
                {
                    var sum = reply.Value.Select(r => r.RoundtripTime).Count(time => time < 0);
                    sw.WriteLine($"address: {reply.Key} (cannot reach {sum} times)");
                }
                else
                {
                    sw.WriteLine($"address: {reply.Key}");
                }

                sw.WriteLine(
                    $"roundtrip time: {CalculateAvgRoundtripTime(reply.Value)}");
            }
        }

        static void WriteToXml(Dictionary<string, List<PingResponse>> replies, string path)
        {
            using var sw = new StreamWriter(path + ".xml");

            sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
            sw.WriteLine("<ping>");
            sw.WriteLine("<totalAvg>");
            sw.WriteLine($"avg roundtrip time: {CalculateAvgRoundtripTime(replies)}");
            sw.WriteLine("</totalAvg>");

            sw.WriteLine("<results>");
            foreach (var reply in replies)
            {
                sw.WriteLine("<result>");

                if (reply.Value.Any(r => r.RoundtripTime < 0))
                {
                    var sum = reply.Value.Select(r => r.RoundtripTime).Count(time => time < 0);
                    sw.WriteLine("<note>");
                    sw.WriteLine($"cannot reach {sum} times");
                    sw.WriteLine("</note>");
                }

                sw.WriteLine("<address>");
                sw.WriteLine(reply.Key);
                sw.WriteLine("</address>");

                sw.WriteLine("<roundtripTime>");
                sw.WriteLine(CalculateAvgRoundtripTime(reply.Value));
                sw.WriteLine("</roundtripTime>");

                sw.WriteLine("</result>");
            }

            sw.WriteLine("</results>");
            sw.WriteLine("</ping>");
        }

        static void WriteToHtml(Dictionary<string, List<PingResponse>> replies, string path)
        {
            using var sw = new StreamWriter(path + ".html");

            sw.WriteLine("<html>");
            sw.WriteLine("<body>");

            sw.WriteLine($"avg roundtrip time: {CalculateAvgRoundtripTime(replies)}");
            sw.WriteLine("<br>");
            sw.WriteLine("<br>");
            foreach (var reply in replies)
            {
                sw.WriteLine("<div>");
                sw.WriteLine();
                if (reply.Value.Any(r => r.RoundtripTime < 0))
                {
                    var sum = reply.Value.Select(r => r.RoundtripTime).Count(time => time < 0);
                    sw.WriteLine($"address: {reply.Key} (cannot reach {sum} times)");
                }
                else
                {
                    sw.WriteLine($"address: {reply.Key}");
                }

                sw.WriteLine("</div>");
                sw.WriteLine("<div>");
                sw.WriteLine(
                    $"roundtrip time: {CalculateAvgRoundtripTime(reply.Value)}");
                sw.WriteLine("</div>");
                sw.WriteLine("<br>");
            }

            sw.WriteLine("</body>");
            sw.WriteLine("</html>");
        }
    }
}