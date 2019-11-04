using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Qmmands;

namespace Administrator.Commands.Modules.Guild
{
    [Name("Guild")]
    [Group("guild")]
    [RequireUserPermissions(GuildPermission.ManageGuild)]
    public sealed class GuildCommands : AdminModuleBase
    {
        [Command("language")]
        public async ValueTask<AdminCommandResult> GetLanguageAsync()
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            return CommandSuccessLocalized("guild_language", args:
                $"{Format.Bold(guild.Language.NativeName)} ({guild.Language.EnglishName}, `{guild.Language.CultureCode}`)");
        }

        [Command("language")]
        public async ValueTask<AdminCommandResult> SetLanguageAsync([Remainder] LocalizedLanguage newLanguage)
        {
            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            guild.Language = newLanguage;
            Context.Database.Guilds.Update(guild);
            await Context.Database.SaveChangesAsync();
            Context.Language = newLanguage;

            return CommandSuccessLocalized("guild_language_set", args:
                $"{Format.Bold(guild.Language.NativeName)} ({guild.Language.EnglishName}, `{guild.Language.CultureCode}`)");
        }

        [Command("languages")]
        public AdminCommandResult GetLanguages()
            => CommandSuccess(new StringBuilder()
                .AppendLine(Localize("available_languages"))
                .AppendJoin('\n',
                    Localization.Languages.Select(
                        x => Format.Code($"{x.NativeName} ({x.EnglishName}, {x.CultureCode})"))).ToString());
    }
}