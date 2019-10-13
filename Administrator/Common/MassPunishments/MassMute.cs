using System;
using System.Globalization;
using CommandLine;

namespace Administrator.Common
{
    public sealed class MassMute : MassPunishment
    {
        private static readonly string[] Formats = {
            "%d'd'%h'h'%m'm'%s's'", //4d3h2m1s
            "%d'd'%h'h'%m'm'",      //4d3h2m
            "%d'd'%h'h'%s's'",      //4d3h  1s
            "%d'd'%h'h'",           //4d3h
            "%d'd'%m'm'%s's'",      //4d  2m1s
            "%d'd'%m'm'",           //4d  2m
            "%d'd'%s's'",           //4d    1s
            "%d'd'",                //4d
            "%h'h'%m'm'%s's'",      //  3h2m1s
            "%h'h'%m'm'",           //  3h2m
            "%h'h'%s's'",           //  3h  1s
            "%h'h'",                //  3h
            "%m'm'%s's'",           //    2m1s
            "%m'm'",                //    2m
            "%s's'",                //      1s
        };

        [Option('d', "duration", Required = false, HelpText = "mass_punishment_duration")]
        public string DurationString { get; private set; }

        public TimeSpan? GetDuration(CultureInfo info) =>
            TimeSpan.TryParseExact(DurationString.ToLower(info), Formats, info, out var result)
                ? result
                : (TimeSpan?) null;
    }
}