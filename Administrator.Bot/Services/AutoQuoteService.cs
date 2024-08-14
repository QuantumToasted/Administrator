using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;

namespace Administrator.Bot;

public sealed partial class AutoQuoteService : DiscordBotService
{
    private static readonly Regex JumpLinkRegex = GenerateJumpLinkRegex();
    
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.GuildId is not { } guildId || string.IsNullOrWhiteSpace(e.Message.Content))
            return;

        //var match = Discord.MessageJumpLinkRegex.Match(e.Message.Content);
        var match = JumpLinkRegex.Match(e.Message.Content);

        // TODO: support multiple matches?
        if (!match.Success)
            return;
        
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var guildConfig = await db.Guilds.GetOrCreateAsync(guildId);
        if (!guildConfig.HasSetting(GuildSettings.AutoQuote) || guildConfig.AutoQuoteExemptChannelIds.Contains(e.ChannelId))
            return;

        var messageGuildId = Snowflake.TryParse(match.Groups["guild_id"].Value, out var id) ? id : (Snowflake?) null;
        var messageChannelId = Snowflake.Parse(match.Groups["channel_id"].Value);
        var messageId = Snowflake.Parse(match.Groups["message_id"].Value);

        string? error = null;
        IUserMessage? message = null;
        if (messageGuildId != guildId)
            error = "You cannot quote messages from other servers, sorry!";
        else if (e.Message.Author is not IMember member || e.Channel is null)
            error = "I could not determine whether or not you have permission to view this message, sorry!";
        else if (!member.CalculateChannelPermissions(e.Channel).HasFlag(Permissions.ViewChannels))
            error = "You do not have permission to view this message, sorry!";
        else
        {
            try
            {
                var m = await Bot.FetchMessageAsync(messageChannelId, messageId);

                if (m is not IUserMessage userMessage)
                    error = "The message you are attempting to quote was not a user or bot's message.";
                else
                    message = userMessage;
            }
            catch (Exception ex)
            {
                error = $"Failed to grab the message you are trying to quote:\n{Markdown.CodeBlock(ex.Message)}";
            }
        }

        LocalMessage localMessage;
        if (!string.IsNullOrWhiteSpace(error))
        {
            localMessage = new LocalMessage().AddEmbed(new LocalEmbed()
                .WithCollectorsColor()
                .WithTitle("Auto-quote failed")
                .WithDescription(error));
        }
        else
        {
            localMessage = message!.ToQuoteMessage(guildId, e.Message.Author, Bot.GetChannel(guildId, message!.ChannelId) as IMessageGuildChannel);
        }

        if (await Bot.TrySendMessageAsync(e.ChannelId, localMessage) is null)
        {
            _ = Bot.AddReactionsAsync(e.ChannelId, e.MessageId, "💬", "❌");
        }
    }

    [GeneratedRegex(@"https?://(?:(ptb|canary)\.)?discord(?:app)?\.com/channels/(?<guild_id>([0-9]{15,21})|(@me))/(?<channel_id>[0-9]{15,21})/(?<message_id>[0-9]{15,21})/?", RegexOptions.Compiled)]
    private static partial Regex GenerateJumpLinkRegex();
}