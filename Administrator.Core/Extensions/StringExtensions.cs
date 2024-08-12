namespace Administrator.Core;

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
    
    public static int GetLevenshteinDistanceTo(this string str, string other)
    {
        //str = str.ToLower();
        //other = other.ToLower();

        var n = str.Length;
        var m = other.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; d[i, 0] = i++)
        { }

        for (var j = 0; j <= m; d[0, j] = j++)
        { }

        for (var i = 1; i <= n; i++)
        for (var j = 1; j <= m; j++)
        {
            var cost = other[j - 1] == str[i - 1] ? 0 : 1;

            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);
        }

        return d[n, m];
    }
}