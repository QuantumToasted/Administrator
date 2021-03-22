using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Administrator.Extensions
{
    public static class HttpExtensions
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
}