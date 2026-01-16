using Humanizer;

namespace Administrator.Bot;

public sealed class RequireNonNitroAttachmentSizeAttribute() : RequireAttachmentSizeUnderAttribute(ByteSize.FromMegabytes(8));