using System;
using System.IO;

namespace Administrator.Common
{
    public sealed class MessageFile : IDisposable
    {
        public MessageFile(Stream stream, string filename)
        {
            Stream = stream;
            Filename = filename;
        }
        
        public Stream Stream { get; }
        
        public string Filename { get; }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}