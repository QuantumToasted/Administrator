using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Discord;

namespace Administrator.Common
{
    public static class Log
    {
        private const int PAD_LENGTH = 24;
        
        public static bool VerboseEnabled { get; set; }

        public static void Critical(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessage(value, source, "CRIT", ConsoleColor.Red, textColor);

        public static void Error(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessage(value, source, "ERRO", ConsoleColor.Yellow, textColor);

        public static void Warning(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessage(value, source, "WARN", ConsoleColor.Magenta, textColor);

        public static void Info(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
            => LogMessage(value, source, "INFO", ConsoleColor.Green, textColor);

        public static void Debug(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
        {
            #if DEBUG
            LogMessage(value, source, "DBUG", ConsoleColor.White, textColor);
            #endif
        }

        public static void Verbose(object value, [CallerFilePath] string source = "",
            ConsoleColor textColor = ConsoleColor.Gray)
        {
            if (VerboseEnabled)
                LogMessage(value, source, "VRBS", ConsoleColor.DarkGray, textColor);
        }
        
        private static void LogMessage(object value, string source, string level, ConsoleColor levelColor,
            ConsoleColor textColor)
        {
            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text)) return;

            foreach (var message in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                Console.Write('[');
                Console.ForegroundColor = levelColor;
                Console.Write(level);
                Console.ResetColor();

                try
                {
                    source = Path.GetFileNameWithoutExtension(new Uri(source).AbsolutePath);
                }
                catch
                {
                    // wasn't a file path, just use the caller name directly
                }

                var spaces = PAD_LENGTH - source.Length;
                var padLeft = spaces / 2 + source.Length;
                Console.Write($"|{source.PadLeft(padLeft).PadRight(PAD_LENGTH)}] ");

                Console.ForegroundColor = textColor;
                Console.WriteLine(message);

            }
        }
    }
}