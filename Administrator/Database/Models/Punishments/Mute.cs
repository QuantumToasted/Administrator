using System;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Database
{
    public sealed class Mute : RevocablePunishment, IEntityTypeConfiguration<Mute>
    {
        public Snowflake? ChannelId { get; set; }

        public ulong? PreviousChannelAllowValue { get; set; }

        public ulong? PreviousChannelDenyValue { get; set; }
        
        public override async Task<LocalMessage> FormatLogMessageAsync(DiscordBotBase bot)
        {
            var builder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle($"Mute - Case #{Id}")
                .WithDescription(
                    $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has been muted.")
                .WithImageUrl(Attachment?.Uri.ToString())
                .AddField("Reason",
                    string.IsNullOrWhiteSpace(Reason)
                        ? $"Moderator: use {Markdown.Code($"reason {Id} <reason>")} to add a reason."
                        : Reason)
                .WithTimestamp(CreatedAt);

            if (ExpiresAt.HasValue)
            {
                builder.AddField("Duration", (ExpiresAt.Value - CreatedAt).HumanizeFormatted());
            }

            if (ChannelId.HasValue && bot.GetChannel(GuildId, ChannelId.Value) is CachedTextChannel channel)
            {
                builder.AddField("Channel", channel.Mention);
            }

            var user = await bot.GetOrFetchUserAsync(ModeratorId);
            return new LocalMessageBuilder()
                .WithEmbed(builder.WithFooter($"Moderator: {ModeratorTag}", user.GetAvatarUrl()))
                .Build();
        }

        public override Task<LocalMessage> FormatDmMessageAsync(DiscordBotBase bot)
        {
            var guild = bot.GetGuild(GuildId);
            var config = bot.Services.GetRequiredService<IConfiguration>();
            
            var builder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle($"Mute - Case #{Id}")
                .WithDescription(
                    $"You have been muted in {Markdown.Bold(guild?.Name ?? "UNKNOWN_GUILD")}.")
                .WithImageUrl(Attachment?.Uri.ToString())
                .AddField("Reason",
                    string.IsNullOrWhiteSpace(Reason)
                        ? Markdown.Italics("No reason specified.")
                        : Reason)
                .WithTimestamp(CreatedAt);
            
            if (ExpiresAt.HasValue)
            {
                builder.AddField("Duration", (ExpiresAt.Value - CreatedAt).HumanizeFormatted());
            }
            
            if (ChannelId.HasValue && bot.GetChannel(GuildId, ChannelId.Value) is CachedTextChannel channel)
            {
                builder.AddField("Channel", channel.Mention);
            }

            builder.AddField("Appealing",
                    $"To appeal this mute, use the command {Markdown.Code($"appeal {Id} [your appeal here]")}.")
                .AddField("Can't send a message?",
                    "If the bot does not accept your messages due to not sharing a server, you can join " +
                    Markdown.Link(config["AppealServer:Name"],
                        $"https://discord.gg/{config["AppealServer:Code"]}") +
                    " to be able to share a server with the bot.\n" +
                    "Or, enable direct messages from server members in your client settings.");

            return Task.FromResult(new LocalMessageBuilder()
                .WithEmbed(builder)
                .Build());
        }
        
        public override async Task<LocalMessage> FormatRevokedMessageAsync(DiscordBotBase bot)
        {
            var builder = new LocalEmbedBuilder()
                .WithWarningColor()
                .WithTitle($"Mute - Case #{Id}")
                .WithDescription(
                    $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has been unmuted.")
                .AddField("Reason",
                    string.IsNullOrWhiteSpace(RevocationReason)
                        ? Markdown.Italics("No reason provided.")
                        : RevocationReason)
                .WithTimestamp(RevokedAt);

            var user = await bot.GetOrFetchUserAsync(RevokerId);
            return new LocalMessageBuilder()
                .WithEmbed(builder.WithFooter($"Moderator: {RevokerTag}", user.GetAvatarUrl()))
                .Build();
        }

        public override Task<LocalMessage> FormatRevokedDmMessageAsync(DiscordBotBase bot)
        {
            var guild = bot.GetGuild(GuildId);

            return Task.FromResult(new LocalMessageBuilder()
                .WithEmbed(new LocalEmbedBuilder()
                    .WithWarningColor()
                    .WithTitle($"Mute - Case #{Id}")
                    .WithDescription(
                        $"You have been unmuted in {Markdown.Bold(guild?.Name ?? "UNKNOWN_GUILD")}.")
                    .AddField("Reason",
                        string.IsNullOrWhiteSpace(RevocationReason)
                            ? Markdown.Italics("No reason specified.")
                            : RevocationReason)
                    .WithTimestamp(RevokedAt))
                .Build());
        }
        
        public override Task<LocalMessage> FormatAppealMessageAsync(DiscordBotBase bot)
        {
            return Task.FromResult(new LocalMessageBuilder()
                .WithEmbed(new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle($"Mute - Case #{Id}")
                    .WithDescription(
                        $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has appealed their mute.")
                    .AddField("Reason", AppealReason)
                    .WithTimestamp(AppealedAt))
                .Build());
        }

        public static Mute Create(IGuild guild, IUser target, IUser moderator, ITextChannel channel = null, IOverwrite previousOverwrite = null, 
            TimeSpan? duration = null, string reason = null, Upload attachment = null)
        {
            return new()
            {
                GuildId = guild.Id,
                TargetId = target.Id,
                TargetTag = target.Tag,
                ModeratorId = moderator.Id,
                ModeratorTag = moderator.Tag,
                Reason = reason,
                Attachment = attachment,
                CreatedAt = DateTimeOffset.UtcNow,
                ChannelId = channel?.Id,
                PreviousChannelAllowValue = previousOverwrite?.Permissions.Allowed,
                PreviousChannelDenyValue = previousOverwrite?.Permissions.Denied,
                ExpiresAt = DateTimeOffset.UtcNow + duration
            };
        }

        void IEntityTypeConfiguration<Mute>.Configure(EntityTypeBuilder<Mute> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}