using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Administrator.Bot;

public static class RegexExtensions
{
    public static bool IsMatch(this Regex regex, string input, [NotNullWhen(true)] out Match? match)
    {
        match = regex.Match(input);
        return match.Success;
    }
}