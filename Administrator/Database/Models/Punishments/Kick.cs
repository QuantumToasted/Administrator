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
    public sealed class Kick : Punishment, IEntityTypeConfiguration<Kick>
    {
        public override async Task<LocalMessage> FormatLogMessageAsync(DiscordBotBase bot)
        {
            var builder = new LocalEmbedBuilder()
                .WithWarningColor()
                .WithTitle($"Kick - Case #{Id}")
                .WithDescription(
                    $"User {Markdown.Bold(TargetTag)} {Markdown.Code(TargetId.ToString())} has been kicked.")
                .WithImageUrl(Attachment?.Uri.ToString())
                .AddField("Reason",
                    string.IsNullOrWhiteSpace(Reason)
                        ? $"Moderator: use {Markdown.Code($"reason {Id} <reason>")} to add a reason."
                        : Reason)
                .WithTimestamp(CreatedAt);

            var user = await bot.GetOrFetchUserAsync(ModeratorId);
            return new LocalMessageBuilder()
                .WithEmbed(builder.WithFooter($"Moderator: {ModeratorTag}", user.GetAvatarUrl()))
                .Build();
        }

        public override Task<LocalMessage> FormatDmMessageAsync(DiscordBotBase bot)
        {
            var guild = bot.GetGuild(GuildId);

            return Task.FromResult(new LocalMessageBuilder()
                .WithEmbed(new LocalEmbedBuilder()
                    .WithWarningColor()
                    .WithTitle($"Kick - Case #{Id}")
                    .WithDescription(
                        $"You have been kicked from {Markdown.Bold(guild?.Name ?? "UNKNOWN_GUILD")}.")
                    .WithImageUrl(Attachment?.Uri.ToString())
                    .AddField("Reason",
                        string.IsNullOrWhiteSpace(Reason)
                            ? Markdown.Italics("No reason specified.")
                            : Reason)
                    .WithTimestamp(CreatedAt))
                .Build());
        }

        public static Kick Create(IGuild guild, IUser target, IUser moderator, string reason = null, Upload attachment = null)
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
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        
        void IEntityTypeConfiguration<Kick>.Configure(EntityTypeBuilder<Kick> builder)
        {
            builder.HasBaseType<Punishment>();
        }
    }
}