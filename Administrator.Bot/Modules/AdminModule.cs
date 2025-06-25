using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("admin")]
[RequireInitialAuthorPermissions(Permissions.Administrator)]
public sealed partial class AdminModule
{
    [SlashCommand("api-key")]
    [Description("Generates a new key for third-party API access.")]
    public partial Task GenerateApiKey();
}