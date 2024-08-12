using System.Buffers.Binary;
using System.IO.Compression;

namespace Administrator.Core;

public static class CompressionExtensions
{
    public static byte[] GZipCompress(this byte[] bytes)
    {
        var size = bytes.Length;
        var sizeBuffer = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(sizeBuffer, size);

        using var memoryStream = new MemoryStream();
        using var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress);
        
        memoryStream.Write(sizeBuffer);
        gzipStream.Write(bytes);
        gzipStream.Flush();

        return memoryStream.ToArray();
    }
	
    public static byte[] GZipDecompress(this byte[] bytes)
    {
        var size = BinaryPrimitives.ReadInt32LittleEndian(bytes); // UNCOMPRESSED size
        var data = new byte[size];

        using var sourceStream = new MemoryStream(bytes, 4, bytes.Length - 4);
        using var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress);
        using var destinationStream = new MemoryStream(data);
        
        gzipStream.CopyTo(destinationStream);
        return data;
    }
}