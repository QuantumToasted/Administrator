using System;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands.Modules.Mail
{
    [Group("modmail", "mm")]
    public sealed class ModmailCommands : AdminModuleBase
    {
        [Command("", "anon", "anonymous")]
        [RequireContext(ContextType.DM)]
        public async ValueTask<AdminCommandResult> OpenModmailAsync([RequireMember] SocketGuild guild,
            [Remainder, MustBe(StringLength.ShorterThan, EmbedBuilder.MaxDescriptionLength)] string message)
        {
            var guildConfig = await Context.Database.GetOrCreateGuildAsync(guild.Id);
            if (guildConfig.BlacklistedModmailAuthors.Contains(Context.User.Id))
            {
                // TODO
                return CommandSuccessLocalized("modmail_send_success",
                    args: await Context.Database.Modmails.CountAsync() + 1);
            }

            if (await Context.Database.Modmails.FirstOrDefaultAsync(x =>
                x.UserId == Context.User.Id && x.GuildId == guild.Id && !x.ClosedBy.HasValue) is Modmail mm)
            {
                return CommandErrorLocalized("modmail_exists", args: new object[]
                {
                    guild.Name,
                    Format.Code($"{Context.Prefix}mm reply {mm.Id} {message.TrimTo(10, true)}"),
                    Format.Code($"{Context.Prefix}mm close {mm.Id}")
                });
            }

            var logChannel = await Context.Database.GetLoggingChannelAsync(guild.Id, LogType.Modmail);
            var modmail = Context.Database.Modmails
                .Add(new Modmail(guild.Id, Context.User.Id, Context.Alias.Contains("anon"), message)).Entity;
            await Context.Database.SaveChangesAsync();

            await logChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(new Color(0x8ED0FF))
                .WithAuthor(new EmbedAuthorBuilder
                {
                    IconUrl =
                        modmail.IsAnonymous ? Context.User.GetDefaultAvatarUrl() : Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    Name = modmail.IsAnonymous ? Context.Localize("modmail_anonymous") : Context.User.ToString()
                })
                .WithDescription(message)
                .WithTitle(Context.Localize("modmail_title", modmail.Id))
                .WithFooter(Context.Localize("modmail_reply_command",
                    Format.Code($"{Context.Prefix}mm reply {modmail.Id} [...]"),
                    Format.Code($"{Context.Prefix}mm close {modmail.Id}")))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build());

            return CommandSuccess(string.Join('\n', Context.Localize("modmail_send_success", modmail.Id),
                Context.Localize("modmail_reply_command",
                    Format.Code($"{Context.Prefix}mm reply {modmail.Id} [...]"),
                    Format.Code($"{Context.Prefix}mm close {modmail.Id}"))));
        }

        [Command("reply")]
        [RequireLoggingChannel(LogType.Modmail, Group = "reply")]
        [RequireContext(ContextType.DM, Group = "reply")]
        public async ValueTask<AdminCommandResult> ReplyToModmailAsync(int id, 
            [Remainder, MustBe(StringLength.ShorterThan, EmbedBuilder.MaxDescriptionLength)] string message)
        {
            var modmail = await Context.Database.Modmails.FindAsync(id);
            if (modmail is null)
                return CommandErrorLocalized("modmail_notfound");

            if (Context.IsPrivate && modmail.UserId != Context.User.Id)
                return CommandErrorLocalized("modmail_notfound");

            if (!Context.IsPrivate && modmail.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("modmail_notfound");

            SocketTextChannel loggingChannel = null;
            if (Context.IsPrivate)
            {
                if (!(await Context.Database.GetLoggingChannelAsync(modmail.GuildId, LogType.Modmail) is SocketTextChannel
                    logChannel))
                    return CommandErrorLocalized("requireloggingchannel_notfound", args: LogType.Modmail);

                if (modmail.ClosedBy.HasValue)
                    return modmail.ClosedBy.Value == ModmailTarget.User
                        ? CommandErrorLocalized("modmail_closed_user")
                        : CommandErrorLocalized("modmail_closed_guild");

                loggingChannel = logChannel;
            }
            else
            {
                if (modmail.ClosedBy.HasValue)
                    return modmail.ClosedBy.Value == ModmailTarget.User
                        ? CommandErrorLocalized("modmail_closed_user_guild")
                        : CommandErrorLocalized("modmail_closed_guild");
            }
            

            Context.Database.ModmailMessages.Add(new ModmailMessage(Context.IsPrivate ? ModmailTarget.User : ModmailTarget.Modteam, message, modmail));
            await Context.Database.SaveChangesAsync();

            if (Context.IsPrivate)
            {
                await loggingChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithColor(new Color(0x8ED0FF))
                    .WithAuthor(new EmbedAuthorBuilder
                    {
                        IconUrl =
                            modmail.IsAnonymous ? Context.User.GetDefaultAvatarUrl() : Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Name = modmail.IsAnonymous ? Context.Localize("modmail_anonymous") : Context.User.ToString()
                    })
                    .WithDescription(message)
                    .WithTitle(Context.Localize("modmail_title", modmail.Id))
                    .WithFooter(Context.Localize("modmail_reply_command",
                        Format.Code($"{Context.Prefix}mm reply {modmail.Id} [...]"),
                        Format.Code($"{Context.Prefix}mm close {modmail.Id}")))
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .Build());
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var user = Context.Client.GetUser(modmail.UserId) ??
                                   await Context.Client.Rest.GetUserAsync(modmail.UserId) as IUser;
                        await user.SendMessageAsync(embed: new EmbedBuilder()
                            .WithColor(new Color(0x8ED0FF))
                            .WithAuthor(new EmbedAuthorBuilder
                            {
                                IconUrl = Context.Guild.IconUrl,
                                Name = $"{Context.Guild.Name} modteam"
                            })
                            .WithDescription(message)
                            .WithTitle(Context.Localize("modmail_title", modmail.Id))
                            .WithFooter(Context.Localize("modmail_reply_command",
                                Format.Code($"{Context.Prefix}mm reply {modmail.Id} [...]"),
                                Format.Code($"{Context.Prefix}mm close {modmail.Id}")))
                            .WithTimestamp(DateTimeOffset.UtcNow)
                            .Build());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                });
            }

            return CommandSuccessLocalized("modmail_reply_success");
        }

        [Command("close")]
        [RequireLoggingChannel(LogType.Modmail, Group = "close")]
        [RequireContext(ContextType.DM, Group = "close")]
        public async ValueTask<AdminCommandResult> CloseModmailAsync(int id)
        {
            var modmail = await Context.Database.Modmails.FindAsync(id);
            if (modmail is null)
                return CommandErrorLocalized("modmail_notfound");

            if (Context.IsPrivate && modmail.UserId != Context.User.Id)
                return CommandErrorLocalized("modmail_notfound");

            if (!Context.IsPrivate && modmail.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("modmail_notfound");

            if (modmail.ClosedBy.HasValue)
                return CommandErrorLocalized("modmail_alreadyclosed");

            modmail.Close(Context.IsPrivate ? ModmailTarget.User : ModmailTarget.Modteam);
            Context.Database.Modmails.Update(modmail);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("modmail_closed_success");
        }

        [Command("blacklist", "toggleblacklist")]
        [RequireLoggingChannel(LogType.Modmail)]
        [Priority(0)]
        public ValueTask<AdminCommandResult> ToggleModmailBlacklist([Remainder] SocketUser target)
            => ToggleModmailBlacklistAsync(target.Id);

        [Command("blacklist", "toggleblacklist")]
        [RequireLoggingChannel(LogType.Modmail)]
        [Priority(1)]
        public async ValueTask<AdminCommandResult> ToggleModmailBlacklistAsync(int id)
        {
            if (!(await Context.Database.Modmails.FindAsync(id) is Modmail modmail) ||
                modmail.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("modmail_notfound");

            return await ToggleModmailBlacklistAsync(modmail.UserId);
        }

        [Command("blacklist", "toggleblacklist")]
        [RequireLoggingChannel(LogType.Modmail)]
        [Priority(0)]
        private async ValueTask<AdminCommandResult> ToggleModmailBlacklistAsync(ulong userId)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            if (!guild.BlacklistedModmailAuthors.Remove(userId))
            {
                guild.BlacklistedModmailAuthors.Add(userId);
            }

            Context.Database.Guilds.Update(guild);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized(guild.BlacklistedModmailAuthors.Contains(userId)
                ? "modmail_blacklist_added"
                : "modmail_blacklist_removed");
        }
    }
}