namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ImageAttribute() : RequireAttachmentExtensionsAttribute("png", "jpg", "jpeg", "gif", "webp");