using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Interaction;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static async ValueTask<TMessage> ToLocalMessageAsync<TMessage>(this Tag tag, DiscordBotBase bot, IDiscordGuildCommandContext? context = null)
        where TMessage : LocalMessageBase, new()
    {
        var message = tag.Message is null
            ? new TMessage()
            : await tag.Message.ToLocalMessageAsync<TMessage>(new DiscordPlaceholderFormatter(), context);

        if (tag.Attachment is not null && await tag.Attachment.DownloadAsync(bot) is { } localAttachment)
        {
            message.AddAttachment(localAttachment);
        }

        return message;
    }
    
    public static LocalEmbed FormatInfoEmbed(this Tag tag, DiscordBotBase bot, List<TagLink> linkedTags)
    {
        var embed = new LocalEmbed()
            .WithUnusualColor()
            .WithDescription(tag.Name)
            .AddField("Total uses", tag.Uses)
            .AddField("Last used", tag.LastUsedAt is { } lastUsedAt
                ? Markdown.Timestamp(lastUsedAt, Markdown.TimestampFormat.RelativeTime) 
                : "(never)")
            .AddField("Created", Markdown.Timestamp(tag.CreatedAt, Markdown.TimestampFormat.RelativeTime));

        if (bot.GetUser(tag.OwnerId) is { } owner)
        {
            embed.WithAuthor($"Owner: {owner.Tag}", owner.GetAvatarUrl(CdnAssetFormat.Automatic));
        }
        else
        {
            embed.WithAuthor($"Owner: {tag.OwnerId}", Discord.Cdn.GetDefaultAvatarUrl(DefaultAvatarColor.Blurple));
        }

        if (tag.Aliases.Length > 0)
        {
            embed.AddField("Aliases", new StringBuilder()
                .AppendJoinTruncated("\n", tag.Aliases, Discord.Limits.Message.Embed.Field.MaxValueLength)
                .ToString());
        }

        if (linkedTags.Count > 0)
        {
            embed.AddField("Linked tags", new StringBuilder()
                .AppendJoinTruncated("\n", linkedTags.Select(x => x.To), Discord.Limits.Message.Embed.Field.MaxValueLength)
                .ToString());
        }

        return embed;
    }
    
    public static Task ShowAsync(this Tag tag, IDiscordInteractionGuildCommandContext context, bool isEphemeral)
        => tag.ShowAsync(context, context.Interaction, isEphemeral);
    
    public static async Task ShowAsync(this Tag tag, IDiscordGuildCommandContext context, IUserInteraction interaction, bool isEphemeral)
    {
        await interaction.Response().DeferAsync(isEphemeral);
        var message = await tag.ToLocalMessageAsync<LocalInteractionMessageResponse>(context.Bot, context);
        message.WithIsEphemeral(isEphemeral);

        await using var scope = context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var links = await db.LinkedTags.Where(x => x.GuildId == context.GuildId && x.From == tag.Name).ToListAsync();

        if (links.Count == 0)
        {
            await interaction.RespondOrFollowupAsync(message);
            return;
        }

        var view = new TagLinkView(context, message, links, isEphemeral);
        var menu = new AdminInteractionMenu(view, interaction);
        await context.Bot.StartMenuAsync(context.ChannelId, menu, TimeSpan.FromMinutes(30));
    }
}