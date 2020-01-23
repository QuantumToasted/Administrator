using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Permission = Disqord.Permission;

namespace Administrator.Commands.Aliases
{
    [Name("Aliases")]
    [Group("alias")]
    [RequireUserPermissions(Permission.ManageGuild)]
    public sealed class AliasCommands : AdminModuleBase
    {
        public CommandService Commands { get; set; }

        public PaginationService Pagination { get; set; }

        [Command("", "create")]
        public async ValueTask<AdminCommandResult> CreateAliasAsync([Trimmed, Lowercase, Replace("\n", "")] string alias,
            [Remainder, Trimmed] string command)
        {
            if (await Context.Database.CommandAliases.FindAsync(Context.Guild.Id.RawValue, alias) is { })
                return CommandErrorLocalized("alias_exists");

            if (Commands.FindCommands(alias).Count > 0)
                return CommandErrorLocalized("alias_aliasexists");

            if (Commands.FindCommands(command).Count == 0)
                return CommandErrorLocalized("alias_commandnotfound");

            Context.Database.CommandAliases.Add(new CommandAlias(Context.Guild.Id, alias, command));
            await Context.Database.SaveChangesAsync();

            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("alias_created_title"))
                .WithDescription(Localize("alias_created_description", $"{Context.Prefix}{alias}", $"{Context.Prefix}{command}"))
                .Build());
        }

        [Command("list")]
        public async ValueTask<AdminCommandResult> ListAliasesAsync()
        {
            var aliases = await Context.Database.CommandAliases.Where(x => x.GuildId == Context.Guild.Id)
                .ToListAsync();

            if (aliases.Count == 0)
                return CommandErrorLocalized("alias_list_none");

            var pages = DefaultPaginator.GeneratePages(aliases, 10, alias => new LocalEmbedFieldBuilder()
                .WithName($"{Context.Prefix}{alias.Alias}")
                .WithValue($"{Context.Prefix}{alias.Command}"), builderFunc: () => new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("alias_list_title", Context.Guild.Name.Sanitize())));

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }

        [Command("remove")]
        public async ValueTask<AdminCommandResult> RemoveAliasAsync([Trimmed, Lowercase, Replace("\n", ""), Remainder] string alias)
        {
            if (!(await Context.Database.CommandAliases.FindAsync(Context.Guild.Id.RawValue, alias) is { } commandAlias))
                return CommandErrorLocalized("alias_remove_notfound");

            Context.Database.CommandAliases.Remove(commandAlias);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("alias_remove");
        }
    }
}