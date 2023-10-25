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

public sealed class DiscordPlaceholderFormatter : IPlaceholderFormatter
{
    private static readonly Regex UserPlaceholderRegex =
        new(@"{user\.(?:xp|level|nextxp|tier|img)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GuildUserPlaceholderRegex =
        new(@"{user\.(?:guildxp|guildlevel|guildnextxp|guildtier|img)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RandomNumberRegex =
        new(@"{random(\d{1,})-(\d{1,})}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async ValueTask<string> ReplacePlaceholdersAsync(string str, IDiscordGuildCommandContext? context)
    {
        if (context is null)
            return str;

        await using var scope = context.Services.CreateAsyncScopeWithDatabase(out var db);
        var xpService = context.Services.GetRequiredService<XpService>();

        // Target - deprecated as custom text commands are no longer a thing
        /*
        if (target is not null)
            str = str.Replace("{target}", target.ToString());
        */

        // Random numbers
        str = RandomNumberRegex.Replace(str, ReplaceRandomNumber);

        // User
        str = str.Replace("{user.nick}", context.Author.GetDisplayName())
            .Replace("{user.joined}", Markdown.Timestamp(context.Author.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow, Markdown.TimestampFormat.RelativeTime))
            .Replace("{user}", context.Author.Tag)
            .Replace("{user.tag}", context.Author.Tag)
            .Replace("{user.id}", context.Author.Id.ToString())
            .Replace("{user.guildavatar}", context.Author.GetGuildAvatarUrl())
            .Replace("{user.avatar}", context.Author.GetAvatarUrl())
            .Replace("{user.name}", context.Author.Name)
            .Replace("{user.mention}", context.Author.Mention)
            .Replace("{user.created}", Markdown.Timestamp(context.Author.CreatedAt(), Markdown.TimestampFormat.RelativeTime));
            //.Replace("{user.discrim}", context.Author.Discriminator);

        if (UserPlaceholderRegex.IsMatch(str))
        {
            var userXp = await db.GetOrCreateGlobalUserAsync(context.AuthorId);
            str = str.Replace("{user.xp}", userXp.CurrentLevelXp.ToString())
                .Replace("{user.level}", userXp.Level.ToString())
                .Replace("{user.nextxp}", userXp.NextLevelXp.ToString())
                .Replace("{user.tier}", userXp.Tier.ToString())
                .Replace("{user.img}", xpService.GetLevelEmoji(userXp.Tier, userXp.Level).GetUrl());
        }

        if (GuildUserPlaceholderRegex.IsMatch(str))
        {
            var guildUserXp = await db.GetOrCreateGlobalUserAsync(context.AuthorId);
            str = str.Replace("{user.guildxp}", guildUserXp.CurrentLevelXp.ToString())
                .Replace("{user.guildlevel}", guildUserXp.Level.ToString())
                .Replace("{user.guildnextxp}", guildUserXp.NextLevelXp.ToString())
                .Replace("{user.guildtier}", guildUserXp.Tier.ToString())
                .Replace("{user.guildimg}", xpService.GetLevelEmoji(guildUserXp.Tier, guildUserXp.Level).GetUrl());
        }

        if (context.Bot.GetChannel(context.GuildId, context.ChannelId) is { } channel)
        {
            str = str.Replace("{channel}", (channel as ITaggableEntity)?.Tag ?? string.Empty)
                .Replace("{channel.tag}", (channel as ITaggableEntity)?.Tag ?? string.Empty)
                .Replace("{channel.id}", channel.Id.ToString())
                .Replace("{channel.name}", channel.Name)
                .Replace("{channel.created}", Markdown.Timestamp(channel.CreatedAt(), Markdown.TimestampFormat.RelativeTime))
                .Replace("{channel.topic}", (channel as ITopicChannel)?.Topic ?? string.Empty)
                .Replace("{channel.mention}", channel.Mention, StringComparison.OrdinalIgnoreCase);
        }

        if (context.Bot.GetGuild(context.GuildId) is { } guild)
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
                return context.Bot.Services.GetRequiredService<Random>()
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
        => ReplacePlaceholdersAsync(str, context as IDiscordGuildCommandContext);
}