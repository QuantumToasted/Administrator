using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("server")]
public sealed class ServerModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("info")]
    [Description("Displays detailed information about this server.")]
    public IResult ShowInfo()
    {
        var guild = Bot.GetGuild(Context.GuildId)!;
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithTitle($"Server information for {guild.Name}")
            .AddField("ID", guild.Id)
            .AddField("Created", Markdown.Timestamp(guild.CreatedAt(), Markdown.TimestampFormat.RelativeTime));

        if (guild.GetIconUrl(CdnAssetFormat.Automatic) is { } iconUrl)
            embed.WithThumbnailUrl(iconUrl);

        var statsField = new LocalEmbedField()
            .WithName("Stats");

        var channels = guild.GetChannels().Values.ToList();
        var valueBuilder = new StringBuilder()
            .AppendNewline($"Approximate member count: {guild.Members.Count}")
            .AppendNewline($"Bots: {guild.Members.Values.Count(x => x.IsBot)}")
            .AppendNewline($"Text channels: {channels.Count(x => x.Type is ChannelType.Text)}")
            .AppendNewline($"Threads: {channels.Count(x => x.Type is ChannelType.PublicThread or ChannelType.PrivateThread or ChannelType.NewsThread)}")
            .AppendNewline($"Voice channels: {channels.Count(x => x.Type is ChannelType.Voice)}")
            .AppendNewline($"Categories: {channels.Count(x => x.Type is ChannelType.Category)}");

        embed.AddField(statsField.WithValue(valueBuilder.ToString()));
        
        return Response(embed);
    }
}