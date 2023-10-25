namespace Administrator.Bot;

public static class StringExtensions
{
    public static string Truncate(this string str, int length, bool useEllipses = true)
    {
        if (str.Length <= length)
            return str;

        return useEllipses
            ? str[..(length - 1)] + '…'
            : str[..length];
    }
}