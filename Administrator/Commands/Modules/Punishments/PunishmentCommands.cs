using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Punishments")]
    public class PunishmentCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        public PunishmentService Punishments { get; set; }

        [RequireUserPermissions(GuildPermission.ManageMessages)]
        public sealed class MainCommands : PunishmentCommands
        {
            [Command("punishments", "cases"), RunMode(RunMode.Parallel)]
            public async ValueTask<AdminCommandResult> ListPunishmentsAsync([MustBe(Operator.GreaterThan, 0)] int page = 1)
            {
                var punishments =
                    await Context.Database.Punishments.Where(x => x.GuildId == Context.Guild.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                if (punishments.Count == 0)
                    return CommandErrorLocalized("punishment_nopunishments_guild");


                var split = punishments.SplitBy(10);

                var tempPaginator = new PunishmentPaginator(null, split, Math.Min(page, split.Count) - 1, Context,
                    Context.Guild.Id, PunishmentListType.Guild, Pagination);
                var firstPage = await tempPaginator.BuildPageAsync();

                if (split.Count > 1)
                {
                    var message = await Pagination.SendPaginatorAsync(Context.Channel, firstPage);
                    await using var paginator = new PunishmentPaginator(message, split, Math.Min(page, split.Count) - 1,
                        Context, Context.Guild.Id, PunishmentListType.Guild, Pagination);
                    await paginator.WaitForExpiryAsync();
                    return CommandSuccess();
                }

                return CommandSuccess(embed: firstPage.Embed.ToEmbedBuilder()
                    .WithFooter((string) null)
                    .Build());
            }

            [Command("punishments", "cases"), RunMode(RunMode.Parallel)]
            public async ValueTask<AdminCommandResult> ListPunishmentsAsync(SocketGuildUser target,
                [MustBe(Operator.GreaterThan, 0)] int page = 1)
            {
                var punishments =
                    await Context.Database.Punishments.Where(x => x.GuildId == Context.Guild.Id && x.TargetId == target.Id)
                        .OrderByDescending(x => x.Id)
                        .ToListAsync();

                if (punishments.Count == 0)
                    return CommandErrorLocalized("punishment_nopunishments_user");

                var split = punishments.SplitBy(10);

                var tempPaginator = new PunishmentPaginator(null, split, Math.Min(page, split.Count) - 1, Context,
                    Context.Guild.Id, PunishmentListType.Guild, Pagination); // TODO: Did IAsyncDisposable changes break this?
                var firstPage = await tempPaginator.BuildPageAsync();

                if (split.Count > 1)
                {
                    var message = await Pagination.SendPaginatorAsync(Context.Channel, firstPage);
                    await using var paginator = new PunishmentPaginator(message, split, Math.Min(page, split.Count) - 1,
                        Context, Context.Guild.Id, PunishmentListType.Guild, Pagination);
                    await paginator.WaitForExpiryAsync();
                    return CommandSuccess();
                }

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

                var builder = new EmbedBuilder()
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
                                ? ban.Duration.Value.HumanizeFormatted(Context, TimeUnit.Second)
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
                                ? mute.Duration.Value.HumanizeFormatted(Context, TimeUnit.Second)
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
                        ? "✅ " + revocable.RevokedAt.Value.ToString("g", Context.Language.Culture) + $" - {Format.Bold(revoker?.ToString() ?? "???")} - " +
                          (revocable.RevocationReason?.TrimTo(920) ?? Context.Localize("punishment_noreason"))
                        : "❌");
                }

                return CommandSuccess(embed: builder.Build());
            }

            [Command("revoke")]
            [RequireBotPermissions(GuildPermission.BanMembers | GuildPermission.ManageRoles)]
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
                            await Context.Guild.RemoveBanAsync(punishment.TargetId);
                        }
                        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
                        {
                            key = "punishment_revoked_nounban";
                        }
                        break;
                    case Mute mute:
                        type = LogType.Unmute;
                        if (!(Context.Guild.GetUser(punishment.TargetId) is SocketGuildUser target))
                            break;
                        if (mute.ChannelId.HasValue)
                        {
                            var channel = Context.Guild.GetTextChannel(mute.ChannelId.Value);
                            if (!channel.PermissionOverwrites.Any(x =>
                                x.TargetId == punishment.TargetId && x.Permissions.SendMessages == PermValue.Deny &&
                                x.Permissions.AddReactions == PermValue.Deny))
                            {
                                key = "punishment_revoked_nounmute";
                                break;
                            }

                            await channel.RemovePermissionOverwriteAsync(target);
                            if (mute.PreviousChannelAllowValue.HasValue)
                            {
                                await channel.AddPermissionOverwriteAsync(target, new OverwritePermissions(
                                    mute.PreviousChannelAllowValue.Value,
                                    mute.PreviousChannelDenyValue.Value));
                            }
                            break;
                        }

                        var muteRole = await Context.Database.GetSpecialRoleAsync(Context.Guild.Id, RoleType.Mute);
                        if (muteRole is null) break;
                        await target.RemoveRoleAsync(muteRole);
                        break;
                    case Warning _:
                        type = LogType.Unwarn;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (await Context.Database.GetLoggingChannelAsync(Context.Guild.Id, type) is SocketTextChannel logChannel)
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

                if (!(Context.Guild.GetTextChannel(punishment.LogMessageChannelId) is SocketTextChannel channel) ||
                    !(await channel.GetMessageAsync(punishment.LogMessageId) is IUserMessage message))
                {
                    return CommandSuccess(string.Join('\n', Context.Localize("punishment_reason_success"), Context.Localize("punishment_reason_missingmessage")));
                }

                await message.ModifyAsync(x =>
                {
                    var builder = message.Embeds.First().ToEmbedBuilder();
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
                if (!(await Context.Database.WarningPunishments.FindAsync(Context.Guild.Id, count) is WarningPunishment
                    punishment))
                    return CommandErrorLocalized("warningpunishment_notfound_count", args: count.ToOrdinalWords(Context.Language.Culture));

                string text;
                switch (punishment.Type)
                {
                    case PunishmentType.Mute:
                        text = punishment.Duration.HasValue
                            ? Context.Localize("warningpunishment_mute_duration",
                                punishment.Duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
                            : Context.Localize("punishment_mute").ToLower();
                        break;
                    case PunishmentType.Kick:
                        text = Context.Localize("punishment_kick").ToLower();
                        break;
                    case PunishmentType.Ban:
                        text = punishment.Duration.HasValue
                            ? Context.Localize("warningpunishment_ban_duration",
                                punishment.Duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
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
                if (!(await Context.Database.WarningPunishments.FindAsync(Context.Guild.Id, count) is WarningPunishment
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
                                duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
                            : Context.Localize("punishment_mute").ToLower();
                        break;
                    case PunishmentType.Kick:
                        text = Context.Localize("punishment_kick").ToLower();
                        break;
                    case PunishmentType.Ban:
                        text = duration.HasValue
                            ? Context.Localize("warningpunishment_ban_duration",
                                duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
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

                var builder = new EmbedBuilder()
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
                                    punishment.Duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
                                : Context.Localize("punishment_mute").ToLower(Context.Language.Culture);
                            break;
                        case PunishmentType.Kick:
                            text = Context.Localize("punishment_kick").ToLower(Context.Language.Culture);
                            break;
                        case PunishmentType.Ban:
                            text = punishment.Duration.HasValue
                                ? Context.Localize("warningpunishment_ban_duration",
                                    punishment.Duration.Value.HumanizeFormatted(Context, TimeUnit.Second))
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