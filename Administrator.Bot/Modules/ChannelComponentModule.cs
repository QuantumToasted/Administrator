using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ChannelComponentModule
{
    [ModalCommand("Channel:Create:Text")]
    public partial Task<IResult> CreateTextChannel(string name, string? topic = null, string? categoryName = null);

    [ModalCommand("Channel:Create:Voice")]
    public partial Task<IResult> CreateVoiceChannel(string name, string? categoryName = null);

    [ModalCommand("Channel:Create:Category")]
    public partial Task<IResult> CreateCategoryChannel(string name);

    [ModalCommand("Channel:Modify:Text:*")]
    public partial Task<IResult> ModifyTextChannel(Snowflake channelId, string name, string? topic = null, string? categoryName = null);

    [ModalCommand("Channel:Modify:Voice:*")]
    public partial Task<IResult> ModifyVoiceChannel(Snowflake channelId, string name, string? categoryName = null);

    [ModalCommand("Channel:Modify:Category:*")]
    public partial Task<IResult> ModifyCategoryChannel(Snowflake channelId, string name);

    [ModalCommand("Channel:Modify:Forum:*")]
    public partial Task<IResult> ModifyForumChannel(Snowflake channelId, string name, string? topic = null, string? categoryName = null);
}