using Disqord.Bot.Commands.Application;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("server")]
public sealed partial class ServerModule
{
    [SlashCommand("info")]
    [Description("Displays detailed information about this server.")]
    public partial IResult ShowInfo();
}