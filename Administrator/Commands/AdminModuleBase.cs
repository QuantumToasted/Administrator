using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Services;
using Discord;
using Qmmands;

namespace Administrator.Commands
{
    public abstract class AdminModuleBase : ModuleBase<AdminCommandContext>, IDisposable
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        
        public LocalizationService Localization { get; set; }

        protected AdminCommandResult CommandSuccess(string text = null, Embed embed = null, MessageFile file = null)
            => new AdminCommandResult(_watch.Elapsed, text, embed, file, true);

        protected AdminCommandResult CommandSuccessLocalized(string key, Embed embed = null, MessageFile file = null,
            params object[] args)
            => new AdminCommandResult(_watch.Elapsed, Localization.Localize(Context.Language, key, args), embed, file,
                true);

        protected AdminCommandResult CommandError(string text, Embed embed = null, MessageFile file = null)
            => new AdminCommandResult(_watch.Elapsed, text, embed, file, false);

        protected AdminCommandResult CommandErrorLocalized(string key, Embed embed = null, MessageFile file = null,
            params object[] args)
            => new AdminCommandResult(_watch.Elapsed, Localization.Localize(Context.Language, key, args), embed, file,
                false);

        protected override async Task BeforeExecutedAsync(Command command)
        {
            if (!Context.IsPrivate)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                Context.SetLanguage(guild.Language);
                return;
            }

            var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
            Context.SetLanguage(user.Language);
        }

        public void Dispose()
        {
            _watch.Stop();
            Context.Dispose();
        }
    }
}