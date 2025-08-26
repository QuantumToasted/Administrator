using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Administrator.Core;

public static class RegexUtil
{
    public const string REGEX_HELPER_SITE = "https://regex101.com/";
    
    public static bool TryCreate(string s, [NotNullWhen(true)] out Regex? regex)
    {
        try
        {
            regex = new Regex(s);
            return true;
        }
        catch
        {
            regex = null;
            return false;
        }
    }
}