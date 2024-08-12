using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Humanizer;
using LinqToDB;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class TagImportComponentModule
{
    [ModalCommand("Tag:Import:*:*")]
    public partial Task<IResult> ImportAsync(Snowflake channelId, Snowflake messageId, string name);
}