using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pinger
{
    public class PingArgs
    {
        public const string UrlsArg = "--urls";
        public const string UrlsFileArg = "--urls-file";
        public const string DelayArg = "--delay-ms";
        public const string TimeoutArgName = "--timeout-ms";
        public const string AttemptsArg = "--attempts";
        public const string FormatArgName = "--format";
        public const string OutputArgName = "-o";

        private readonly string[] _args;
        private string[] _urls;

        public PingArgs(string[] args)
        {
            _args = args;
        }

        public IEnumerable<string> Urls => _urls ??= ParseUrls();

        public int Delay => _args.GetInt(DelayArg, true) ?? 0;

        public int Attempts => _args.GetInt(AttemptsArg, true) ?? 1;

        public OutputFormats Formats => ValidateFormat(_args.GetStrings(FormatArgName, new[]
        {
            UrlsArg,
            UrlsFileArg,
            DelayArg,
            AttemptsArg,
            OutputArgName,
            TimeoutArgName,
            FormatArgName
        }, true));

        public string OutputFileName => _args.GetString(OutputArgName, true) ?? "output";

        public int Timeout => _args.GetInt(TimeoutArgName, true) ?? 200;

        string[] ParseUrls()
        {
            if (_args.GetString(UrlsArg, true) is not null)
            {
                return _args.GetStrings(UrlsArg, new[]
                {
                    UrlsFileArg,
                    DelayArg,
                    UrlsArg,
                    AttemptsArg,
                    FormatArgName,
                    OutputArgName,
                    TimeoutArgName
                }).ToArray();
            }

            if (_args.GetString(UrlsFileArg, true) is not null)
            {
                var file = _args.GetString(UrlsFileArg);
                if (!file.EndsWith(".txt") || !File.Exists(file))
                {
                    throw new ArgumentException($"{UrlsFileArg} is not a valid .txt file");
                }

                return GetUrlsFromFile(file);
            }

            throw new InvalidOperationException($"{UrlsFileArg} or {UrlsArg} should be provided");
        }

        string[] GetUrlsFromFile(string path)
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

        OutputFormats ValidateFormat(IEnumerable<string> formats)
        {
            if (formats is null)
            {
                return OutputFormats.Console;
            }

            var outputFormats = OutputFormats.None;

            foreach (var format in formats.Select(f => f.ToLower()))
            {
                var names = Enum.GetNames<OutputFormats>().Select(n => n.ToLower());
                if (names.Contains(format))
                {
                    outputFormats |= Enum.Parse<OutputFormats>(format, true);
                }
            }

            return outputFormats;
        }
    }

    static class ArgsReader
    {
        public static string GetString(this string[] args, string argName, bool optional = false)
        {
            var indexOfArgName = Array.IndexOf(args, argName);

            if (indexOfArgName == -1 || indexOfArgName == args.Length)
            {
                if (optional)
                {
                    return null;
                }

                throw new ArgumentException($"{argName} should be provided");
            }

            return args[indexOfArgName + 1];
        }

        public static int? GetInt(this string[] args, string argName, bool optional = false)
        {
            var indexOfArgName = Array.IndexOf(args, argName);

            if (indexOfArgName == -1 || indexOfArgName == args.Length)
            {
                if (optional)
                {
                    return null;
                }

                throw new ArgumentException($"{argName} should be provided");
            }

            if (int.TryParse(args[indexOfArgName + 1], out var result))
            {
                return result;
            }

            throw new ArgumentException($"{argName} should be valid integer");
        }

        public static IEnumerable<string> GetStrings(this string[] args, string argName, string[] argNames,
            bool optional = false)
        {
            var indexOfArgName = Array.IndexOf(args, argName);

            if (indexOfArgName == -1 || indexOfArgName == args.Length)
            {
                if (optional)
                {
                    return null;
                }

                throw new ArgumentException($"{argName} should be provided");
            }

            var urls = new List<string>();
            var c = indexOfArgName + 1;
            while (c < args.Length && !argNames.Any(n => n.Equals(args[c])))
            {
                urls.Add(args[c]);
                c++;
            }

            return urls;
        }

        public static bool HasFlag(this string[] args, string flag)
        {
            return args.Contains(flag);
        }
    }
}