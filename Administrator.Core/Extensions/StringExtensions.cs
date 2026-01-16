using System.Text.RegularExpressions;
using Disqord;
using NodaTime;
using NodaTime.Extensions;
using Qommon;

namespace Administrator.Core;

public static class StringExtensions
{
    private static readonly Regex RandomNumberRegex = new(@"{random(\d{1,10})-(\d{1,10})}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    extension(string str)
    {
        public string Truncate(int length)
        {
            return length < str.Length
                ? str[..(length - 1)] + '…'
                : str;
        }
        
        public string ReplaceUserPlaceholders<TUser>(TUser user) where TUser : IUser
        {
            var joinedAt = (user as IMember)?.JoinedAt.GetValueOrNullable()?.ToInstant() ?? Instant.Now;
            return str.Replace("{user.nick}", user.DisplayName)
                .Replace("{user.joined}", Markdown.Timestamp(joinedAt))
                .Replace("{user}", user.Tag)
                .Replace("{user.tag}", user.Tag)
                .Replace("{user.id}", user.Id.ToString())
                .Replace("{user.guildavatar}", (user as IMember)?.GetGuildAvatarUrl())
                .Replace("{user.avatar}", user.GetAvatarUrl())
                .Replace("{user.name}", user.Name)
                .Replace("{user.mention}", user.Mention)
                .Replace("{user.created}", Markdown.Timestamp(user.CreatedAt(), Markdown.TimestampFormat.RelativeTime));
        }

        public string ReplaceUserXpPlaceholders<TXp>(TXp userXp) where TXp : IUserXp
        {
            Guard.IsNull(userXp.GuildId);
            
            return str.Replace("{user.xp}", userXp.CurrentLevelXp.ToString())
                .Replace("{user.level}", userXp.Level.ToString())
                .Replace("{user.nextxp}", userXp.NextLevelXp.ToString())
                .Replace("{user.tier}", userXp.Tier.ToString());
            //.Replace("{user.img}", emojis.GetLevelEmoji(userXp.GetTier(), userXp.GetLevel()).GetUrl()); TODO: restore functionality somehow?
        }

        public string ReplaceMemberXpPlaceholders<TXp>(TXp memberXp) where TXp : IUserXp
        {
            Guard.IsNotNull(memberXp.GuildId);

            return str.Replace("{user.guildxp}", memberXp.CurrentLevelXp.ToString())
                .Replace("{user.guildlevel}", memberXp.Level.ToString())
                .Replace("{user.guildnextxp}", memberXp.NextLevelXp.ToString())
                .Replace("{user.guildtier}", memberXp.Tier.ToString());
            //.Replace("{user.guildimg}", emojis.GetLevelEmoji(guildUserXp.GetTier(), guildUserXp.GetLevel()).GetUrl()); TODO: restore functionality somehow?
        }

        public string ReplaceChannelPlaceholders<TChannel>(TChannel channel) where TChannel : IGuildChannel
        {
            return str.Replace("{channel}", (channel as ITaggableEntity)?.Tag ?? string.Empty)
                .Replace("{channel.tag}", (channel as ITaggableEntity)?.Tag ?? string.Empty)
                .Replace("{channel.id}", channel.Id.ToString())
                .Replace("{channel.name}", channel.Name)
                .Replace("{channel.created}", Markdown.Timestamp(channel.CreatedAt(), Markdown.TimestampFormat.RelativeTime))
                .Replace("{channel.topic}", (channel as ITopicChannel)?.Topic ?? string.Empty)
                .Replace("{channel.mention}", channel.Mention, StringComparison.OrdinalIgnoreCase);
        }

        public string ReplaceGuildPlaceholders<TGuild>(TGuild guild) where TGuild : IGuild
        {
            return str.Replace("{guild}", guild.Name)
                .Replace("{guild.id}", guild.Id.ToString())
                .Replace("{guild.name}", guild.Name)
                .Replace("{guild.created}", Markdown.Timestamp(guild.CreatedAt(), Markdown.TimestampFormat.RelativeTime));
            //.Replace("{guild.members}", guild.Members.Count.ToString(), StringComparison.OrdinalIgnoreCase); TODO: restore functionality somehow?
        }

        public string ReplaceTagPlaceholders<TTag>(TTag tag) where TTag : ITag
        {
            return str.Replace("{tag.name}", tag.Name)
                .Replace("{tag.uses}", tag.Uses.ToString())
                .Replace("{tag.used}", tag.LastUsedAt.HasValue ? Markdown.Timestamp(tag.LastUsedAt.Value) : "never")
                .Replace("{tag.owner}", tag.OwnerId.ToString())
                .Replace("{tag.created}", Markdown.Timestamp(tag.Timestamp));
        }

        public string ReplaceRandomPlaceholders()
        {
            return RandomNumberRegex.Replace(str, ReplaceRandomNumber);
            
            string ReplaceRandomNumber(Match match)
            {
                try
                {
                    return Random.Shared
                        .Next(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value))
                        .ToString();
                }
#pragma warning disable CA1031
                catch
#pragma warning restore CA1031
                {
                    return match.Value;
                }
            }
        }
    }
}