using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Disqord;
using Disqord.Bot.Commands.Parsers;

namespace Administrator.Bot;

public static class ColorExtensions
{
    private static readonly ColorTypeParser Parser = new();
    
    extension(Color)
    {
        public static bool TryParse(string? s, [NotNullWhen(true)] out Color? color)
        {
            if (string.IsNullOrEmpty(s))
            {
                color = null;
                return false;
            }
            
            s = s.TrimStart('#');

            if (int.TryParse(s, NumberStyles.HexNumber, null, out var raw) ||
                int.TryParse(s, out raw))
            {
                color = new Color(raw);
                return true;
            }

            if (Parser.SpacedNames.TryGetValue(s, out var name))
                s = name;

            if (Parser.Colors.TryGetValue(s, out var parserColor))
            {
                color = parserColor;
                return true;
            }

            color = null;
            return false;
        }
    }
}