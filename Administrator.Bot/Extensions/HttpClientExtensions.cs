namespace Administrator.Bot;

public static class HttpClientExtensions
{
    public static Task<MemoryStream> GetMemoryStreamAsync(this HttpClient http, string url)
        => http.GetMemoryStreamAsync(new Uri(url));

    public static async Task<MemoryStream> GetMemoryStreamAsync(this HttpClient http, Uri uri)
    {
        var output = new MemoryStream();
        await using var stream = await http.GetStreamAsync(uri);
        await stream.CopyToAsync(output);
        output.Seek(0, SeekOrigin.Begin);
        return output;
    }
}