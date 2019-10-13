using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Services;
using Discord;
using Discord.WebSocket;
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

        protected string Localize(string key, params object[] args)
            => Context.Localize(key, args);

        protected async Task<SocketUserMessage> GetNextMessageAsync(Func<SocketUserMessage, bool> func = null,
            TimeSpan? timeout = null)
        {
            func ??= x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id;

            var completionSource = new TaskCompletionSource<SocketUserMessage>();
            
            Context.Client.MessageReceived += HandleMessageReceived;

            var sourceTask = completionSource.Task;
            var delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(30));
            var task = await Task.WhenAny(sourceTask, delay);

            Context.Client.MessageReceived -= HandleMessageReceived;

            return task == sourceTask
                ? await sourceTask
                : null;

            Task HandleMessageReceived(SocketMessage message)
            {
                if (message is SocketUserMessage userMessage && func(userMessage))
                    completionSource.SetResult(userMessage);

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