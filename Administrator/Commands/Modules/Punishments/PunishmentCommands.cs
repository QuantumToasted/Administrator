using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Rest;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Permission = Disqord.Permission;

namespace Administrator.Commands
{
    [Name("Punishments")]
    public class PunishmentCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        public PunishmentService Punishments { get; set; }

        [RequireUserPermissions(Permission.ManageMessages)]
        public sealed class MainCommands : PunishmentCommands
        {
            [Command("punishments", "cases")]
            public async ValueTask<AdminCommandResult> ListPunishmentsAsync([MustBe(Operator.GreaterThan, 0)] int page = 1)
            {
                var punishments =
                    await Context.Database.Punishments.Where(x => x.GuildId == Context.Guild.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                if (punishments.Count == 0)
                    return CommandErrorLocalized("punishment_nopunishments_guild");


                var split = punishments.SplitBy(10);
                page = Math.Min(page, punishments.Count) - 1;

                var paginator = new PunishmentPaginator(split, page, Context.Guild.Id, PunishmentListType.Guild, Context);
                var firstPage = await paginator.BuildPageAsync();
                if (split.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, paginator, firstPage);
                    return CommandSuccess();
                }

                // TODO: Empty footer?
                return CommandSuccess(embed: firstPage.Embed);
            }

            [Command("punishments", "cases")]
            public ValueTask<AdminCommandResult> ListPunishments(CachedMember member,
                [MustBe(Operator.GreaterThan, 0)] int page = 1)
                => ListPunishmentsAsync(member.Id, page);

            [Command("punishments", "cases")]
            public async ValueTask<AdminCommandResult> ListPunishmentsAsync(ulong targetId,
                [MustBe(Operator.GreaterThan, 0)] int page = 1)
            {
                var punishments =
                    await Context.Database.Punishments.Where(x => x.GuildId == Context.Guild.Id && x.TargetId == targetId)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                if (punishments.Count == 0)
                    return CommandErrorLocalized("punishment_nopunishments_user");

                var split = punishments.SplitBy(10);
                page = Math.Min(page, punishments.Count) - 1;

                var paginator = new PunishmentPaginator(split, page, targetId, PunishmentListType.User, Context);
                var firstPage = await paginator.BuildPageAsync();
                if (split.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, paginator, firstPage);
                    return CommandSuccess();
                }

                // TODO: Empty footer?
                return CommandSuccess(embed: firstPage.Embed);
            }

            [Command("punishment", "case")]
            public async ValueTask<AdminCommandResult> ShowPunishmentAsync([MustBe(Operator.GreaterThan, 0)] int id)
            {
                var punishment =
                    await Context.Database.Punishments.FirstOrDefaultAsync(x =>
                        x.GuildId == Context.Guild.Id && x.Id == id);

                if (punishment is null)
                    return CommandErrorLocalized("punishment_notfound_id");


                var target = await Context.Client.GetOrDownloadUserAsync(punishment.TargetId);
                var moderator = await Context.Client.GetOrDownloadUserAsync(punishment.ModeratorId);

                var builder = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(Context.Localize($"punishment_{punishment.GetType().Name.ToLower()}") +
                               $" - {Context.Localize("punishment_case", punishment.Id)}")
                    .AddField(Context.Localize("punishment_target_title"), target?.Format(false) ?? "???", true)
                    .AddField(Context.Localize("punishment_moderator_title"), moderator?.Format(false) ?? "???", true)
                    .AddField(Context.Localize("punishment_timestamp_title"),
                        punishment.CreatedAt.ToString("g", Context.Language.Culture), true)
                    .AddField(Context.Localize("title_reason"),
                        punishment.Reason ?? Context.Localize("punishment_noreason"));

                switch (punishment)
                {
                    case Ban ban:
                        builder.AddField(Context.Localize("punishment_duration"),
                            ban.Duration.HasValue
                                ? ban.Duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second)
                                : Context.Localize("punishment_permanent"));
                        break;
                    case Mute mute:
                        if (mute.ChannelId.HasValue)
                        {
                            builder.AddField(Context.Localize("punishment_mute_channel"),
                                Context.Guild.GetTextChannel(mute.ChannelId.Value)?.Mention ?? "???");
                        }
                        builder.AddField(Context.Localize("punishment_duration"),
                            mute.Duration.HasValue
                                ? mute.Duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second)
                                : Context.Localize("punishment_permanent"));
                        break;
                    case Warning warning when warning.SecondaryPunishmentId.HasValue:
                        var secondary = await Context.Database.Punishments.FindAsync(warning.SecondaryPunishmentId.Value);
                        builder.AddField(Punishments.FormatAdditionalPunishment(secondary, Context.Language));
                        break;
                }

                if (punishment is RevocablePunishment revocable)
                {
                    if (revocable.IsAppealable)
                    {
                        builder.AddField(Context.Localize("punishment_appealed"), revocable.AppealedAt.HasValue
                            ? $"✅ {revocable.AppealedAt.Value.ToString("g", Context.Language.Culture)} - {revocable.AppealReason.TrimTo(950)}"
                            : "❌");
                    }

                    var revoker = revocable.RevokedAt.HasValue
                        ? await Context.Client.GetOrDownloadUserAsync(revocable.RevokerId)
                        : default;

                    builder.AddField(Context.Localize("punishment_revoked"), revocable.RevokedAt.HasValue
                        ? "✅ " + revocable.RevokedAt.Value.ToString("g", Context.Language.Culture) + $" - {Markdown.Bold(revoker?.Tag ?? "???")} - " +
                          (revocable.RevocationReason?.TrimTo(920) ?? Context.Localize("punishment_noreason"))
                        : "❌");
                }

                if (punishment.Format != ImageFormat.Default)
                {
                    builder.WithImageUrl($"attachment://attachment.{punishment.Format.ToString().ToLower()}");
                    return CommandSuccess(embed: builder.Build(), attachment: new LocalAttachment(punishment.Image,
                        $"attachment.{punishment.Format.ToString().ToLower()}"));
                }

                return CommandSuccess(embed: builder.Build());
            }

            [Command("revoke")]
            [RequireBotPermissions(Permission.BanMembers | Permission.ManageRoles)]
            public async ValueTask<AdminCommandResult> RevokePunishmentAsync([MustBe(Operator.GreaterThan, 0)] int id,
                [Remainder] string reason = null)
            {
                var punishment =
                    await Context.Database.Punishments.FirstOrDefaultAsync(x =>
                        x.GuildId == Context.Guild.Id && x.Id == id) as RevocablePunishment;

                if (punishment is null)
                    return CommandErrorLocalized("punishment_notfound_id");

                if (punishment.RevokedAt.HasValue)
                    return CommandErrorLocalized("punishment_alreadyrevoked");


                punishment.Revoke(Context.User.Id, reason);
                Context.Database.Punishments.Update(punishment);
                await Context.Database.SaveChangesAsync();

                var key = "punishment_revoked_success";
                LogType type;
                switch (punishment)
                {
                    case Ban _:
                        type = LogType.Unban;
                        try
                        {
                            await Context.Guild.UnbanMemberAsync(punishment.TargetId);
                        }
                        catch (DiscordHttpException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
                        {
                            key = "punishment_revoked_nounban";
                        }
                        break;
                    case Mute mute:
                        type = LogType.Unmute;
                        if (!(Context.Guild.GetMember(punishment.TargetId) is CachedMember target))
                            break;
                        if (mute.ChannelId.HasValue)
                        {
                            var channel = Context.Guild.GetTextChannel(mute.ChannelId.Value);
                            if (!channel.Overwrites.Any(x =>
                                x.TargetId == punishment.TargetId && x.Permissions.Denied.SendMessages &&
                                x.Permissions.Denied.AddReactions))
                            {
                                key = "punishment_revoked_nounmute";
                                break;
                            }

                            await channel.DeleteOverwriteAsync(target.Id);
                            if (mute.PreviousChannelAllowValue.HasValue)
                            {
                                await channel.AddOrModifyOverwriteAsync(new LocalOverwrite(target,
                                    new OverwritePermissions(mute.PreviousChannelAllowValue.Value,
                                        mute.PreviousChannelDenyValue.Value)));
                            }
                            break;
                        }

                        var muteRole = await Context.Database.GetSpecialRoleAsync(Context.Guild.Id, RoleType.Mute);
                        if (muteRole is null) break;
                        await target.RevokeRoleAsync(muteRole.Id);
                        break;
                    case Warning _:
                        type = LogType.Unwarn;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, type) is CachedTextChannel logChannel)
                {
                    var target = await Context.Client.GetOrDownloadUserAsync(punishment.TargetId);
                    var moderator = await Context.Client.GetOrDownloadUserAsync(punishment.RevokerId);
                    await Punishments.SendLoggingRevocationEmbedAsync(punishment, target, moderator, logChannel,
                        Context.Language);
                    _ = Punishments.SendTargetRevocationEmbedAsync(punishment, target, Context.Language);
                }

                return CommandSuccessLocalized(key);
            }

            [Command("reason")]
            public async ValueTask<AdminCommandResult> AddReasonAsync([MustBe(Operator.GreaterThan, 0)] int id,
                [Remainder] string reason)
            {
                var punishment =
                    await Context.Database.Punishments.FirstOrDefaultAsync(x =>
                        x.GuildId == Context.Guild.Id && x.Id == id);

                if (punishment is null)
                    return CommandErrorLocalized("punishment_notfound_id");

                if (!string.IsNullOrEmpty(punishment.Reason))
                    return CommandErrorLocalized("punishment_reason_exists");

                punishment.Reason = reason;
                Context.Database.Punishments.Update(punishment);
                await Context.Database.SaveChangesAsync();

                if (!(Context.Guild.GetTextChannel(punishment.LogMessageChannelId) is CachedTextChannel channel) ||
                    !(await channel.GetMessageAsync(punishment.LogMessageId) is IUserMessage message))
                {
                    return CommandSuccess(string.Join('\n', Context.Localize("punishment_reason_success"), Context.Localize("punishment_reason_missingmessage")));
                }

                await message.ModifyAsync(x =>
                {
                    var builder = LocalEmbedBuilder.FromEmbed(message.Embeds[0]);
                    var field = builder.Fields.FirstOrDefault(y => y.Name.Equals(Context.Localize("title_reason")));
                    if (!(field is null))
                    {
                        builder.Fields.First(y => y.Name.Equals(field.Name)).Value = reason;
                    }
                    x.Embed = builder.Build();
                });

                if (channel.Id == Context.Channel.Id)
                {
                    _ = Context.Message.DeleteAsync();
                    return CommandSuccess();
                }

                return CommandSuccess("punishment_reason_success");
            }

            [Command("warningpunishment", "warnp")]
            public async ValueTask<AdminCommandResult> GetWarningPunishmentAsync([MustBe(Operator.GreaterThan, 0)] int count)
            {
                if (!(await Context.Database.WarningPunishments.FindAsync(Context.Guild.Id.RawValue, count) is WarningPunishment
                    punishment))
                    return CommandErrorLocalized("warningpunishment_notfound_count", args: count.ToOrdinalWords(Context.Language.Culture));

                string text;
                switch (punishment.Type)
                {
                    case PunishmentType.Mute:
                        text = punishment.Duration.HasValue
                            ? Context.Localize("warningpunishment_mute_duration",
                                punishment.Duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                            : Context.Localize("punishment_mute").ToLower();
                        break;
                    case PunishmentType.Kick:
                        text = Context.Localize("punishment_kick").ToLower();
                        break;
                    case PunishmentType.Ban:
                        text = punishment.Duration.HasValue
                            ? Context.Localize("warningpunishment_ban_duration",
                                punishment.Duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                            : Context.Localize("punishment_ban").ToLower();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return CommandSuccessLocalized("warningpunishment_show", args: new object[] { count, text });
            }

            [Command("warningpunishment", "warnp")]
            public async ValueTask<AdminCommandResult> SetWarningPunishmentAsync([MustBe(Operator.GreaterThan, 0)] int count,
                PunishmentType type, TimeSpan? duration = null)
            {
                if (!(await Context.Database.WarningPunishments.FindAsync(Context.Guild.Id.RawValue, count) is WarningPunishment
                    punishment))
                {
                    Context.Database.WarningPunishments.Add(new WarningPunishment(Context.Guild.Id, count, type, duration));
                    await Context.Database.SaveChangesAsync();
                }
                else
                {
                    punishment.Type = type;
                    punishment.Duration = duration;
                    Context.Database.WarningPunishments.Update(punishment);
                    await Context.Database.SaveChangesAsync();
                }

                string text;
                switch (type)
                {
                    case PunishmentType.Mute:
                        text = duration.HasValue
                            ? Context.Localize("warningpunishment_mute_duration",
                                duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                            : Context.Localize("punishment_mute").ToLower();
                        break;
                    case PunishmentType.Kick:
                        text = Context.Localize("punishment_kick").ToLower();
                        break;
                    case PunishmentType.Ban:
                        text = duration.HasValue
                            ? Context.Localize("warningpunishment_ban_duration",
                                duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                            : Context.Localize("punishment_ban").ToLower();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return CommandSuccessLocalized("warningpunishment_show", args: new object[] { count, text });
            }

            [Command("warningpunishments", "warnps")]
            public async ValueTask<AdminCommandResult> GetWarningPunishmentsAsync()
            {
                var punishments = await Context.Database.WarningPunishments.Where(x => x.GuildId == Context.Guild.Id)
                    .OrderBy(x => x.Count).ToListAsync();

                if (punishments.Count == 0)
                    return CommandErrorLocalized("warningpunishments_none");

                var builder = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(Context.Localize("warningpunishments_title", Context.Guild.Name));

                var sb = new StringBuilder();

                foreach (var punishment in punishments)
                {
                    sb.Append($"{punishment.Count} => ");
                    string text;
                    switch (punishment.Type)
                    {
                        case PunishmentType.Mute:
                            text = punishment.Duration.HasValue
                                ? Context.Localize("warningpunishment_mute_duration",
                                    punishment.Duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                                : Context.Localize("punishment_mute").ToLower(Context.Language.Culture);
                            break;
                        case PunishmentType.Kick:
                            text = Context.Localize("punishment_kick").ToLower(Context.Language.Culture);
                            break;
                        case PunishmentType.Ban:
                            text = punishment.Duration.HasValue
                                ? Context.Localize("warningpunishment_ban_duration",
                                    punishment.Duration.Value.HumanizeFormatted(Localization, Context.Language, TimeUnit.Second))
                                : Context.Localize("punishment_ban").ToLower(Context.Language.Culture);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    sb.AppendLine(char.ToUpper(text[0], Context.Language.Culture) + text[1..]);
                }

                builder.WithDescription(sb.ToString());

                return CommandSuccess(embed: builder.Build());
            }
        }

        [Command("appeal")]
        [RequireContext(ContextType.DM)]
        public async ValueTask<AdminCommandResult> AppealAsync([MustBe(Operator.GreaterThan, 0)] int id,
            [Remainder, MustBe(StringLength.ShorterThan, 512)] string reason)
        {
            var punishment =
                await Context.Database.Punishments.OfType<RevocablePunishment>().FirstOrDefaultAsync(x =>
                    x.TargetId == Context.User.Id && x.Id == id && x.IsAppealable);

            if (punishment is null)
                return CommandErrorLocalized("punishment_notfound_id");

            if (punishment.AppealedAt.HasValue)
                return CommandErrorLocalized("punishment_alreadyappealed");

            punishment.Appeal(reason);
            Context.Database.Punishments.Update(punishment);
            await Context.Database.SaveChangesAsync();
            await Punishments.LogAppealAsync(Context.User, Context.Client.GetGuild(punishment.GuildId), punishment);

            return CommandSuccessLocalized("punishment_appeal_success");
        }
    }
}