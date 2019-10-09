using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Modmail")]
    [Group("modmail", "mm")]
    public sealed class ModmailCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        [Command("", "anon", "anonymous")]
        [RequireContext(ContextType.DM)]
        public async ValueTask<AdminCommandResult> OpenModmailAsync([RequireMember] SocketGuild guild,
            [Remainder, MustBe(StringLength.ShorterThan, EmbedBuilder.MaxDescriptionLength)] string message)
        {
            var guildConfig = await Context.Database.GetOrCreateGuildAsync(guild.Id);
            if (guildConfig.BlacklistedModmailAuthors.Contains(Context.User.Id))
            {
                return CommandSuccessLocalized("modmail_send_success",
                    args: await Context.Database.Modmails.CountAsync() + 1);
            }

            if (await Context.Database.Modmails.FirstOrDefaultAsync(x =>
                x.UserId == Context.User.Id && x.GuildId == guild.Id && !x.ClosedBy.HasValue) is Modmail mm)
            {
                return CommandErrorLocalized("modmail_exists", args: new object[]
                {
                    guild.Name,
                    Format.Code($"{Context.Prefix}mm reply {mm.Id} {message.TrimTo(20, true)}"),
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
                        modmail.IsAnonymous ? Context.User.GetDefaultAvatarUrl() : Context.User.GetAvatarOrDefault(),
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
                            modmail.IsAnonymous ? Context.User.GetDefaultAvatarUrl() : Context.User.GetAvatarOrDefault(),
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
                        var user = await Context.Client.GetOrDownloadUserAsync(modmail.UserId);
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
        public async ValueTask<AdminCommandResult> ToggleModmailBlacklistAsync(ulong userId)
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

        [Command("show", "thread")]
        [RequireLoggingChannel(LogType.Modmail, Group = "show")]
        [RequireContext(ContextType.DM, Group = "show")]
        public async ValueTask<AdminCommandResult> ShowModmailThreadAsync(int id, [MustBe(Operator.GreaterThan, 0)] int page = 1)
        {
            var modmail = await Context.Database.Modmails.Include(x => x.Messages).FirstOrDefaultAsync(x => x.Id == id);
            if (modmail is null)
                return CommandErrorLocalized("modmail_notfound");

            if (Context.IsPrivate && modmail.UserId != Context.User.Id)
                return CommandErrorLocalized("modmail_notfound");

            if (!Context.IsPrivate && modmail.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("modmail_notfound");

            var split = modmail.Messages.OrderBy(x => x.Timestamp)
                .ToList().SplitBy(10);
            page = Math.Min(page, split.Count) - 1;

            var guild = Context.IsPrivate ? Context.Client.GetGuild(modmail.GuildId) : Context.Guild;
            var user = await Context.Client.GetOrDownloadUserAsync(modmail.UserId);

            var sb = new StringBuilder();
            var lastTarget = ModmailTarget.User;
            var pages = new List<Page>();
            var counter = 0;
            foreach (var group in split)
            {
                var builder = new EmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(Context.Localize("modmail_message_title", modmail.Id));

                if (split.Count > 1)
                    builder.WithFooter($"{++counter}/{split.Count}");

                for (var i = 0; i < group.Count; i++)
                {
                    var message = group[i];

                    if (message.Target != lastTarget && i > 0)
                    {
                        if (Context.IsPrivate)
                        {
                            builder.AddField(lastTarget == ModmailTarget.User
                                    ? Context.User.Username
                                    : Context.Localize("modmail_modteam", guild.Name),
                                sb.ToString().TrimTo(256, true));
                        }
                        else
                        {
                            builder.AddField(lastTarget == ModmailTarget.User
                                    ? modmail.IsAnonymous ? Context.Localize("modmail_anonymous") : user.Username
                                    : Context.Localize("modmail_modteam", guild.Name),
                                sb.ToString().TrimTo(256, true));
                        }

                        sb.Clear();
                        i--;
                    }
                    else
                    {
                        sb.AppendLine(message.Text);
                    }

                    lastTarget = message.Target;

                    if (i == group.Count - 1)
                    {
                        if (Context.IsPrivate)
                        {
                            builder.AddField(lastTarget == ModmailTarget.User
                                    ? Context.User.Username
                                    : Context.Localize("modmail_modteam", guild.Name),
                                sb.ToString().TrimTo(256, true));
                        }
                        else
                        {
                            builder.AddField(lastTarget == ModmailTarget.User
                                    ? modmail.IsAnonymous ? Context.Localize("modmail_anonymous") : user.Username
                                    : Context.Localize("modmail_modteam", guild.Name),
                                sb.ToString().TrimTo(256, true));
                        }
                    }
                }

                pages.Add(builder.Build());
            }

            if (split.Count > 1)
            {
                var message = await Pagination.SendPaginatorAsync(Context.Channel, pages[page]);
                Pagination.AddPaginator(new DefaultPaginator(message, pages, page));
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }
    }
}