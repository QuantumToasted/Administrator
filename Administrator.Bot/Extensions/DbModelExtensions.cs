using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qommon;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static string FormatKey<T>(this INumberKeyedDbEntity<T> entity)
        where T : INumber<T>
    {
        return $"`[#{entity.Id}]`";
    }

    public static bool IsImageAttachment(this Attachment attachment)
        => Path.GetExtension(attachment.FileName) is { } extension && new[] { "png", "jpeg", "jpg", "webp" }.Contains(extension[1..], StringComparer.InvariantCultureIgnoreCase); 
    
    public static TUser IncrementXp<TUser>(this TUser user, int xp, TimeSpan xpGainInterval, out bool leveledUp)
        where TUser : User
    {
        leveledUp = false;
        
        if (DateTimeOffset.UtcNow < user.LastXpGain + xpGainInterval)
            return user;

        var currentLevel = user.Level;
        user = user with { TotalXp = user.TotalXp + xp, LastXpGain = DateTimeOffset.UtcNow };

        if (currentLevel != user.Level)
        {
            leveledUp = true;
            user = user with { LastLevelUp = DateTimeOffset.UtcNow };
        }

        return user;
    }

    public static bool HasSetting(this Guild guild, GuildSettings setting)
        => guild.Settings.HasFlag(setting);

    public static Task ApplyAsync(this RoleLevelReward reward, IMember member)
        => member.ModifyAsync(x => x.RoleIds = Optional.Create(member.RoleIds.Except(reward.RevokedRoleIds.Select(static y => new Snowflake(y)))
            .Concat(reward.GrantedRoleIds.Select(static y => new Snowflake(y)))));

    public static Task RevokeAsync(this RoleLevelReward reward, IMember member)
        => member.ModifyAsync(x => x.RoleIds = Optional.Create(member.RoleIds.Except(reward.GrantedRoleIds.Select(static y => new Snowflake(y)))
            .Concat(reward.RevokedRoleIds.Select(static y => new Snowflake(y)))));

    public static LocalAttachment ToLocalAttachment(this Attachment attachment)
    {
        var output = new MemoryStream(attachment.Data);
        output.Seek(0, SeekOrigin.Begin);
        return new LocalAttachment(output, attachment.FileName);
    }
    
    public static async ValueTask<TMessage> ToLocalMessageAsync<TMessage>(this Tag tag, IDiscordGuildCommandContext? context = null)
        where TMessage : LocalMessageBase, new()
    {
        var message = tag.Message is null
            ? new TMessage()
            : await tag.Message.ToLocalMessageAsync<TMessage>(new DiscordPlaceholderFormatter(), context);

        if (tag.Attachment is not null)
        {
            message.AddAttachment(tag.Attachment.ToLocalAttachment());
        }

        return message;
    }
    
    public static LocalEmbed FormatInfoEmbed(this Tag tag, DiscordBotBase bot)
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

        return embed;
    }
    
    public static string RegenerateApiKey(this Guild guild)
    {
        var idBytes = Encoding.Unicode.GetBytes(guild.Id.ToString());
        var cryptoBytes = new byte[32];
        var saltBytes = new byte[16];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(cryptoBytes);
            rng.GetBytes(saltBytes);
        }

        var pbkdf2 = new Rfc2898DeriveBytes(cryptoBytes, saltBytes, 10000, HashAlgorithmName.SHA256);

        var hash = pbkdf2.GetBytes(32);
        var hashBytes = new byte[48];
        
        Array.Copy(saltBytes, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        guild.ApiKeySalt = saltBytes;
        guild.ApiKeyHash = hashBytes;

        return new StringBuilder()
            .Append(Convert.ToBase64String(idBytes))
            .Append('.')
            .Append(Convert.ToBase64String(cryptoBytes))
            .ToString();
    }

    public static LocalMessage FormatExpiryMessage(this Reminder reminder)
    {
        var contentBuilder = new StringBuilder(Mention.User(reminder.AuthorId));
        contentBuilder.AppendNewline(reminder.RepeatMode.HasValue
            ? $", your reminder {reminder} for every {Markdown.Code(reminder.FormatRepeatDuration())}:"
            : $", your reminder {reminder} from {Markdown.Timestamp(reminder.CreatedAt, Markdown.TimestampFormat.RelativeTime)}:");
        
        contentBuilder.Append(reminder.Text);
        return new LocalMessage()
            .WithContent(contentBuilder.ToString())
            .WithAllowedMentions(new LocalAllowedMentions().WithUserIds(reminder.AuthorId));
    }
    
    public static string FormatRepeatDuration(this Reminder reminder)
    {
        if (!reminder.RepeatMode.HasValue)
            throw new InvalidOperationException("Only repeating reminders can be formatted in this way.");

        var value = reminder.RepeatMode.Value switch
        {
            ReminderRepeatMode.Hourly => TimeSpan.FromHours(reminder.RepeatInterval!.Value),
            ReminderRepeatMode.Daily => TimeSpan.FromDays(reminder.RepeatInterval!.Value),
            ReminderRepeatMode.Weekly => TimeSpan.FromDays(reminder.RepeatInterval!.Value * 7),
            _ => throw new ArgumentOutOfRangeException()
        };

        return value.Humanize();
        //return value.Humanize(int.MaxValue, maxUnit: TimeUnit.Week, minUnit: TimeUnit.Minute);
    }

    public static TimeZoneInfo GetTimeZone(this GlobalUser user)
        => user.TimeZone ?? TimeZoneInfo.Utc;
}