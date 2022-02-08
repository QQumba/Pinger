using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Pinger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var url = args.GetArg("--url");
            var repeat = args.GetArg("-r");
            if (repeat is not null && int.TryParse(repeat, out var r))
            {
                Ping(url, r);
                return;
            }
            
            Ping(url);
        }

        static void Ping(string url, int repeat = 1)
        {
            var ping = new Ping();

            for (var i = 0; i < repeat; i++)
            {
                var reply = ping.Send(url);
                if (reply is {Status: IPStatus.Success})
                {
                    Console.WriteLine($"reply from: {reply.Address}");
                    Console.WriteLine($"round trip times in milli-seconds: {reply.RoundtripTime}");
                }

                if (reply != null && reply.Status != IPStatus.Success)
                {
                    Console.WriteLine("cannot be reached");
                }
            }
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

        public static bool HasFlag(this string[] args, string flag)
        {
            return args.Contains(flag);
        }
    }
}