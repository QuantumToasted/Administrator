using System.Text.RegularExpressions;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

[ScopedService]
public sealed class DiscordPlaceholderFormatter : IPlaceholderFormatter
{
    private static readonly Regex UserPlaceholderRegex =
        new(@"{user\.(?:xp|level|nextxp|tier|img)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GuildUserPlaceholderRegex =
        new(@"{user\.(?:guildxp|guildlevel|guildnextxp|guildtier|img)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RandomNumberRegex =
        new(@"{random(\d{1,10})-(\d{1,10})}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async ValueTask<string> ReplacePlaceholdersAsync(string str, IDiscordCommandContext? context)
    {
        if (context is null)
            return str;

        await using var scope = context.Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        var xpService = context.Bot.Services.GetRequiredService<XpService>();
        var emojis = context.Bot.Services.GetRequiredService<EmojiService>();

        // Target - deprecated as custom text commands are no longer a thing
        /*
        if (target is not null)
            str = str.Replace("{target}", target.ToString());
        */

        // Random numbers
        str = RandomNumberRegex.Replace(str, ReplaceRandomNumber);

        // User
        str = str.Replace("{user.nick}", (context.Author as IMember)?.GetDisplayName() ?? context.Author.Tag)
            .Replace("{user.joined}", Markdown.Timestamp((context.Author as IMember)?.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow, Markdown.TimestampFormat.RelativeTime))
            .Replace("{user}", context.Author.Tag)
            .Replace("{user.tag}", context.Author.Tag)
            .Replace("{user.id}", context.Author.Id.ToString())
            .Replace("{user.guildavatar}", (context.Author as IMember)?.GetGuildAvatarUrl())
            .Replace("{user.avatar}", context.Author.GetAvatarUrl())
            .Replace("{user.name}", context.Author.Name)
            .Replace("{user.mention}", context.Author.Mention)
            .Replace("{user.created}", Markdown.Timestamp(context.Author.CreatedAt(), Markdown.TimestampFormat.RelativeTime));
            //.Replace("{user.discrim}", context.Author.Discriminator);

        if (UserPlaceholderRegex.IsMatch(str))
        {
            var userXp = await db.Users.GetOrCreateAsync(context.AuthorId);
            str = str.Replace("{user.xp}", userXp.CurrentLevelXp.ToString())
                .Replace("{user.level}", userXp.Level.ToString())
                .Replace("{user.nextxp}", userXp.NextLevelXp.ToString())
                .Replace("{user.tier}", userXp.Tier.ToString())
                .Replace("{user.img}", emojis.GetLevelEmoji(userXp.Tier, userXp.Level).GetUrl());
        }

        if (GuildUserPlaceholderRegex.IsMatch(str) && context.GuildId.HasValue)
        {
            var guildUserXp = await db.Members.GetOrCreateAsync(context.GuildId.Value, context.AuthorId);
            str = str.Replace("{user.guildxp}", guildUserXp.CurrentLevelXp.ToString())
                .Replace("{user.guildlevel}", guildUserXp.Level.ToString())
                .Replace("{user.guildnextxp}", guildUserXp.NextLevelXp.ToString())
                .Replace("{user.guildtier}", guildUserXp.Tier.ToString())
                .Replace("{user.guildimg}", emojis.GetLevelEmoji(guildUserXp.Tier, guildUserXp.Level).GetUrl());
        }

        if (context.Bot.TryGetAnyGuildChannel(context.ChannelId, out var channel))
        {
            str = str.Replace("{channel}", (channel as ITaggableEntity)?.Tag ?? string.Empty)
                .Replace("{channel.tag}", (channel as ITaggableEntity)?.Tag ?? string.Empty)
                .Replace("{channel.id}", channel.Id.ToString())
                .Replace("{channel.name}", channel.Name)
                .Replace("{channel.created}", Markdown.Timestamp(channel.CreatedAt(), Markdown.TimestampFormat.RelativeTime))
                .Replace("{channel.topic}", (channel as ITopicChannel)?.Topic ?? string.Empty)
                .Replace("{channel.mention}", channel.Mention, StringComparison.OrdinalIgnoreCase);
        }

        if (context.GuildId.HasValue && context.Bot.GetGuild(context.GuildId.Value) is { } guild)
        {
            str = str.Replace("{guild}", guild.Name)
                .Replace("{guild.id}", guild.Id.ToString())
                .Replace("{guild.name}", guild.Name)
                .Replace("{guild.created}", Markdown.Timestamp(guild.CreatedAt(), Markdown.TimestampFormat.RelativeTime))
                .Replace("{guild.members}", guild.Members.Count.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return str;

        string ReplaceRandomNumber(Match match)
        {
            try
            {
                return Random.Shared
                    .Next(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))
                    .ToString();
            }
            catch
            {
                return match.Value;
            }
        }
    }
    
    ValueTask<string> IPlaceholderFormatter.ReplacePlaceholdersAsync(string str, ICommandContext? context)
        => ReplacePlaceholdersAsync(str, context as IDiscordCommandContext);
}