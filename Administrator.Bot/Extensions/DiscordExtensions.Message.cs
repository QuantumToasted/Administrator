using Administrator.Core;
using Disqord;

namespace Administrator.Bot;

public static partial class DiscordExtensions
{
    public static LocalMessage ToQuoteMessage(this IUserMessage message, Snowflake? guildId, IUser? quoter = null, IMessageGuildChannel? channel = null)
    {
        var localMessage = new LocalMessage()
            .AddComponent(LocalComponent.Row(
                LocalComponent.LinkButton(Discord.MessageJumpLink(guildId, message.ChannelId, message.Id), "Jump to message")));
        
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithAuthor(message.Author.Name, message.Author.GetAvatarUrl())
            .WithTimestamp(message.CreatedAt());

        embed = quoter switch
        {
            IMember quoterMember => embed.WithFooter($"Quoted by {quoterMember.GetDisplayName()}", quoterMember.GetGuildAvatarUrl()),
            not null => embed.WithFooter($"Quoted by {quoter.Name}", quoter.GetAvatarUrl()),
            _ => embed
        };

        if (message.Author is IMember member)
        {
            embed.WithAuthor(member.GetDisplayName(), member.GetGuildAvatarUrl());
            
            if (member.GetHighestRole(x => x.Color.HasValue) is { Color: { } memberNameColor })
                embed.WithColor(memberNameColor);
        }

        if (channel is not null)
            embed.Author.Value.Name = $"{embed.Author.Value.Name.Value} - in {channel.Tag}";
        
        var content = !string.IsNullOrWhiteSpace(message.Content)
            ? message.Content
            : message.Embeds.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Description))?.Description ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(content))
            embed.WithDescription(content.Truncate(Discord.Limits.Message.Embed.MaxDescriptionLength));
        
        var imageUrl = message.Attachments.FirstOrDefault(x => new Uri(x.Url).HasImageExtension()) is { } attachment
            ? attachment.Url
            : message.Embeds.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Image?.Url))?.Image!.Url ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(imageUrl))
            embed.WithImageUrl(imageUrl);

        return localMessage.AddEmbed(embed);
    }
}