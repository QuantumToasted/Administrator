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
    public sealed class Ban : RevocablePunishment, IEntityTypeConfiguration<Ban>
    {
        public override async Task<LocalMessage> FormatLogMessageAsync(DiscordBotBase bot)
        {
            var builder = new LocalEmbedBuilder()
                .WithErrorColor()
                .WithTitle($"Ban - Case #{Id}")
                .WithDescription(
                    $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has been banned.")
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
                .WithTitle($"Ban - Case #{Id}")
                .WithDescription(
                    $"You have been banned from {Markdown.Bold(guild?.Name ?? "UNKNOWN_GUILD")}.")
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

            builder.AddField("Appealing",
                    $"To appeal this ban, use the command {Markdown.Code($"appeal {Id} [your appeal here]")}.")
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
                .WithTitle($"Ban - Case #{Id}")
                .WithDescription(
                    $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has been unbanned.")
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
                    .WithTitle($"Ban - Case #{Id}")
                    .WithDescription(
                        $"You have been unbanned from {Markdown.Bold(guild?.Name ?? "UNKNOWN_GUILD")}.")
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
                    .WithTitle($"Ban - Case #{Id}")
                    .WithDescription(
                        $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has appealed their ban.")
                    .AddField("Reason", AppealReason)
                    .WithTimestamp(AppealedAt))
                .Build());
        }
        
        public static Ban Create(IGuild guild, IUser target, IUser moderator, TimeSpan? duration = null, 
            string reason = null, Upload attachment = null)
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
                ExpiresAt = DateTimeOffset.UtcNow + duration
            };
        }

        void IEntityTypeConfiguration<Ban>.Configure(EntityTypeBuilder<Ban> builder)
        {
            builder.HasBaseType<RevocablePunishment>();
        }
    }
}