using Disqord;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class BotCanViewChannelAttribute() : RequireAuthorChannelPermissionsAttribute(Permissions.ViewChannels);