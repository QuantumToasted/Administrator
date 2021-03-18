using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Disqord;

namespace Administrator.Common
{
    public sealed class Upload : IDisposable
    {
        private const int CHUNK_SIZE = 4096;
        private static readonly HttpClient Http = new();
        private readonly MemoryStream _stream = new();
        
        public Uri Uri { get; private init; }

        public string Filename => Path.GetFileNameWithoutExtension(Uri.LocalPath);

        public string Extension => Path.GetExtension(Uri.LocalPath)[1..];

        public MemoryStream Stream
        {
            get
            {
                if (_stream.Length == 0)
                    throw new InvalidOperationException("This upload's stream has not been downloaded.");

                return _stream;
            }
        }

        public async ValueTask<MemoryStream> GetStreamAsync()
        {
            if (_stream.Length > 0)
                return _stream;

            await using var stream = await Http.GetStreamAsync(Uri);
            await stream.CopyToAsync(_stream);
            _stream.Seek(0, SeekOrigin.Begin);

            return _stream;
        }

        public async Task<bool> VerifySizeAsync(long maxSizeInBytes)
        {
            var totalBytesRead = 0;
            await using var stream = await Http.GetStreamAsync(Uri);
            var buffer = MemoryPool<byte>.Shared.Rent(CHUNK_SIZE);

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.Memory);
                totalBytesRead += bytesRead;

                if (totalBytesRead > maxSizeInBytes)
                {
                    _stream.SetLength(0);
                    return false;
                }

                await _stream.WriteAsync(buffer.Memory.Slice(0, bytesRead));

                if (bytesRead == 0)
                {
                    // We're at the end of the stream. Rewind the backing stream and break out.
                    _stream.Seek(0, SeekOrigin.Begin);
                    break;
                }
            }

            return true;
        }

        public override string ToString()
            => Uri.ToString();

        public void Dispose()
            => _stream.Dispose();
        
        public static bool TryParse(string str, out Upload upload)
        {
            if (Uri.TryCreate(str, UriKind.Absolute, out var result))
            {
                var filename = Path.GetFileName(result.LocalPath);

                if (filename.Contains('.') &&
                    (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
                {
                    upload = new Upload { Uri = result };
                    return true;
                }
            }

            upload = null;
            return false;
        }

        public static Upload Parse(string str) => TryParse(str, out var upload)
            ? upload
            : throw new FormatException("Failed to parse an attachment or upload URL.");

        public static implicit operator Upload(Attachment attachment)
            => new() {Uri = new Uri(attachment.Url)};
    }
}