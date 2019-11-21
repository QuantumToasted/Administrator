using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Administrator.Services;
using Disqord;
using Disqord.Events;
using Qmmands;

namespace Administrator.Commands
{
    public abstract class AdminModuleBase : ModuleBase<AdminCommandContext>, IDisposable
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        
        public LocalizationService Localization { get; set; }

        protected AdminCommandResult CommandSuccess(string text = null, LocalEmbed embed = null, LocalAttachment attachment = null)
            => new AdminCommandResult(_watch.Elapsed, text, embed, attachment, true);

        protected AdminCommandResult CommandSuccessLocalized(string key, LocalEmbed embed = null, LocalAttachment attachment = null,
            params object[] args)
            => new AdminCommandResult(_watch.Elapsed, Localization.Localize(Context.Language, key, args), embed, attachment,
                true);

        protected AdminCommandResult CommandError(string text, LocalEmbed embed = null, LocalAttachment attachment = null)
            => new AdminCommandResult(_watch.Elapsed, text, embed, attachment, false);

        protected AdminCommandResult CommandErrorLocalized(string key, LocalEmbed embed = null, LocalAttachment attachment = null,
            params object[] args)
            => new AdminCommandResult(_watch.Elapsed, Localization.Localize(Context.Language, key, args), embed, attachment,
                false);

        protected string Localize(string key, params object[] args)
            => Context.Localize(key, args);

        protected async Task<CachedUserMessage> GetNextMessageAsync(Func<CachedUserMessage, bool> func = null,
            TimeSpan? timeout = null)
        {
            func ??= x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id;

            var completionSource = new TaskCompletionSource<CachedUserMessage>();
            
            Context.Client.MessageReceived += HandleMessageReceived;

            var sourceTask = completionSource.Task;
            var delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(30));
            var task = await Task.WhenAny(sourceTask, delay);

            Context.Client.MessageReceived -= HandleMessageReceived;

            return task == sourceTask
                ? await sourceTask
                : null;

            Task HandleMessageReceived(MessageReceivedEventArgs args)
            {
                if (args.Message is CachedUserMessage message && func(message))
                    completionSource.SetResult(message);

                return Task.CompletedTask;
            }
        }

        public void Dispose()
        {
            _watch.Stop();
            Context.Dispose();
        }
    }
}