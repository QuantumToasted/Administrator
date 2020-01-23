using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Qmmands;
using Permission = Disqord.Permission;

namespace Administrator.Commands.ReactionRoles
{
    [Name("ReactionRoles")]
    [Group("reactionrole", "rr")]
    [RequireUserPermissions(Permission.ManageRoles, Group = "user")]
    [RequireUserPermissions(Permission.ManageGuild, Group = "user")]
    public sealed class ReactionRoleCommands : AdminModuleBase
    {
        public PaginationService Pagination { get; set; }

        [Command("list")]
        public async ValueTask<AdminCommandResult> ListReactionRolesAsync()
        {
            var reactionRoles = await Context.Database.ReactionRoles.Where(x => x.GuildId == Context.Guild.Id)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            if (reactionRoles.Count == 0)
                return CommandErrorLocalized("reactionrole_list_none");

            var dict = new Dictionary<ulong, ITextChannel>();
            foreach (var role in reactionRoles.DistinctBy(x => x.MessageId))
            {
                dict.Add(role.ChannelId, Context.Guild.GetTextChannel(role.ChannelId));
            }

            var pages = DefaultPaginator.GeneratePages(reactionRoles, 10, role => new LocalEmbedFieldBuilder()
                .WithName($"{role.Emoji} => {Context.Guild.GetRole(role.RoleId).Name.Sanitize()}")
                .WithValue($"{role.Id} - #{dict[role.ChannelId].Name} ({role.MessageId})"), builderFunc: () => new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("reactionrole_list", Context.Guild.Name.Sanitize())));

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }

        [Command("add", "create")]
        public ValueTask<AdminCommandResult> AddReactionRole(ulong messageId, 
            [RequireUsableEmoji] IEmoji emoji, [Remainder, RequireHierarchy] CachedRole role)
            => AddReactionRoleAsync((CachedTextChannel) Context.Channel, messageId, emoji, role);

        [Command("add", "create")]
        public async ValueTask<AdminCommandResult> AddReactionRoleAsync(CachedTextChannel channel, ulong messageId, 
            [RequireUsableEmoji] IEmoji emoji, [Remainder, RequireHierarchy] CachedRole role)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            if (await Context.Database.ReactionRoles.CountAsync(x => x.GuildId == guild.Id) >=
                guild.MaximumReactionRoles)
                return CommandErrorLocalized("reactionrole_add_maximum", args: guild.MaximumReactionRoles);

            var id = 0;
            if (await Context.Database.ReactionRoles.FirstOrDefaultAsync(x =>
                x.MessageId == messageId && x.Emoji.Equals(emoji)) is { } reactionRole)
            {
                reactionRole.RoleId = role.Id;
                Context.Database.ReactionRoles.Update(reactionRole);
                await Context.Database.SaveChangesAsync();
                id = reactionRole.Id;
            }
            else
            {
                var message = await channel.GetMessageAsync(messageId);
                if (message is null)
                    return CommandErrorLocalized("reactionrole_add_nomessage");

                _ = message.AddReactionAsync(emoji);

                reactionRole = Context.Database.ReactionRoles.Add(new ReactionRole(Context.Guild.Id, channel.Id, messageId, role.Id,
                    emoji)).Entity;
                await Context.Database.SaveChangesAsync();
                id = reactionRole.Id;
            }

            return CommandSuccessLocalized("reactionrole_add", args: new object[]
            {
                Markdown.Code($"[{id}]"),
                messageId,
                channel.Mention,
                emoji.ToString(),
                role.Format()
            });
        }

        [Command("remove", "delete")]
        public async ValueTask<AdminCommandResult> RemoveReactionRole([MustBe(Operator.GreaterThan, 0)] int id)
        {
            if (!(await Context.Database.ReactionRoles.FindAsync(id) is { } reactionRole) ||
                reactionRole.GuildId != Context.Guild.Id)
                return CommandErrorLocalized("reactionrole_remove_notfound");

            Context.Database.ReactionRoles.Remove(reactionRole);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("reactionrole_remove", args: id);
        }
    }
}