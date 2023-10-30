using Qommon;

namespace Administrator.Bot;

public static class UriExtensions
{
    public static (string? Filename, string? Extension) GetFileMetadata(this Uri uri)
    {
        var filename = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
        var extension = Path.GetExtension(uri.AbsolutePath);
        return (filename, extension);
    }

    public static bool HasImageExtension(this Uri uri)
        => uri.HasAnyExtension("png", "jpeg", "jpg", "webp", "gif");

    public static bool HasAnyExtension(this Uri uri, params string[] extensions)
    {
        Guard.IsNotEmpty(extensions);
        var (_, extension) = uri.GetFileMetadata();
        return extensions.Contains(extension ?? string.Empty, StringComparer.Ordinal);
    }
}