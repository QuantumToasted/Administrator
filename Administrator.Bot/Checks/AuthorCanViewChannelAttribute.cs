using Disqord;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AuthorCanViewChannelAttribute() : RequireAuthorChannelPermissionsAttribute(Permissions.ViewChannels);