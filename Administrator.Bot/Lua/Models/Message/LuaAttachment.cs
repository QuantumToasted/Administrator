using Disqord;

namespace Administrator.Bot;

public sealed class LuaAttachment(IAttachment attachment) : ILuaModel<LuaAttachment>
{
    public long Id { get; } = (long)attachment.Id.RawValue;

    public string Filename { get; } = attachment.FileName;

    public int Size { get; } = attachment.FileSize;

    public string Url { get; } = attachment.Url;
}