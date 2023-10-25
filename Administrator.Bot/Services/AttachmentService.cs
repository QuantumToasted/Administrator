using Disqord;
using System.Buffers;
using System.Collections.Concurrent;
using Administrator.Core;

namespace Administrator.Bot;

[ScopedService]
public sealed class AttachmentService(HttpClient http) : IDisposable
{
    private const int CHUNK_SIZE = 4096;
    private readonly ConcurrentDictionary<string, Attachment> _attachments = new();

    public ValueTask<Attachment> GetAttachmentAsync(IAttachment attachment)
        => GetAttachmentAsync(attachment.Url);

    public async ValueTask<Attachment> GetAttachmentAsync(string url)
    {
        if (_attachments.Remove(url, out var attachment))
            return attachment;

        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var uri = new Uri(url);
        var filename = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
        var extension = Path.GetExtension(uri.AbsolutePath).ToLower();

        if (string.IsNullOrEmpty(filename) && string.IsNullOrWhiteSpace(extension))
            throw new FormatException($"The url {url} was unable to be mapped to a valid filename and extension.");

        var output = new MemoryStream();
        await stream.CopyToAsync(output);
        output.Seek(0, SeekOrigin.Begin);
        return _attachments[url] = new Attachment(output, $"{filename}{extension}");
    }

    public async Task<bool> CheckSizeAsync(string url, long maxSizeInBytes)
    {
        var totalBytesRead = 0;
        await using var stream = await http.GetStreamAsync(url);
        var buffer = MemoryPool<byte>.Shared.Rent(CHUNK_SIZE);

        var output = new MemoryStream();
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer.Memory);
            totalBytesRead += bytesRead;

            if (totalBytesRead > maxSizeInBytes)
            {
                output.SetLength(0);
                return false;
            }

            await output.WriteAsync(buffer.Memory[..bytesRead]);

            if (bytesRead == 0)
            {
                // We're at the end of the stream. Rewind the backing stream, store the attachment, and break out.
                output.Seek(0, SeekOrigin.Begin);

                var uri = new Uri(url);
                var filename = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
                var extension = Path.GetExtension(uri.AbsolutePath).ToLower();
                _attachments[url] = new Attachment(output, $"{filename}{extension}");
                break;
            }
        }

        return true;
    }

    void IDisposable.Dispose()
    {
        foreach (var attachment in _attachments.Values)
        {
            ((IDisposable)attachment).Dispose();
        }
    }

    public sealed record Attachment(MemoryStream Stream, string FileName) : IDisposable
    {
        void IDisposable.Dispose() => Stream.Dispose();
        
        public static implicit operator LocalAttachment(Attachment attachment) 
            => LocalAttachment.Bytes(attachment.Stream.ToArray(), attachment.FileName);
    }
}