using Administrator.Services;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Qmmands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Administrator.Extensions;
using Disqord;

namespace Administrator.Commands
{
    [Name("Owner")]
    [RequireOwner]
    public sealed class OwnerCommands : AdminModuleBase
    {
        public CancellationTokenSource TokenSource { get; set; }

        public LoggingService Logging { get; set; }

        [Command("eval"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> EvalCodeAsync([Remainder] string script)
        {
            script = script.Replace("```csharp", string.Empty).Replace("```cs", string.Empty).Replace("```", string.Empty);
            script = Regex.Replace(script.Trim(), "^`|`$", string.Empty);
            if (!script.EndsWith('}') && !script.EndsWith(';'))
                script += ';';

            var msg = await Context.Channel.SendMessageAsync(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("owner_eval_inprogress"))
                .WithDescription($"```cs\n{script}\n```")
                .Build());

            try
            {
                var watch = Stopwatch.StartNew();
                var scriptOptions = ScriptOptions.Default
                    .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text",
                        "System.Threading.Tasks", "Disqord", "Administrator.Extensions",
                        "Administrator.Database", "Humanizer")
                    .WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                        .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));

                var result =
                    await CSharpScript.EvaluateAsync(script, scriptOptions, new Globals { Context = Context });

                await msg.ModifyAsync(x => x.Embed = new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(Localize("owner_eval_complete"))
                    .WithDescription(Markdown.CodeBlock(script, "cs"))
                    .AddField(Localize("owner_eval_return"), result is { }
                        ? Markdown.Code(result.GetType().ToString()) + '\n' + result
                        : Localize("info_none"), true)
                    .AddField(Localize("owner_eval_executiontime"), $"{watch.ElapsedMilliseconds / 1000D:F}s", true)
                    .Build());
            }
            catch (Exception ex)
            {
                if (ex is CompilationErrorException e)
                {
                    await msg.ModifyAsync(x => x.Embed = new LocalEmbedBuilder()
                        .WithErrorColor()
                        .WithTitle(Localize("owner_eval_failed"))
                        .AddField(Localize("owner_eval_errors"),
                            Markdown.CodeBlock(string.Join('\n', e.Diagnostics.Select(y => y.GetMessage()))))
                        .Build());
                }
                else
                {
                    await msg.ModifyAsync(x => x.Embed = new LocalEmbedBuilder()
                        .WithErrorColor()
                        .WithTitle(Localize("owner_eval_failed"))
                        .AddField(Localize("owner_eval_errors"),
                            Markdown.CodeBlock(ex.Message))
                        .Build());
                }
            }

            return CommandSuccess();
        }

        [Command("forcestop")]
        public async ValueTask<AdminCommandResult> StopBotAsync()
        {
            await Context.Channel.SendMessageAsync(Localize("owner_forcestop"));
            await Logging.LogCriticalAsync($"Bot shut down by owner {Context.User} [{Context.User.Id}]", "Administrator");
            TokenSource.Cancel();

            return CommandSuccess();
        }
        
        public class Globals
        {
            public AdminCommandContext Context { get; set; }
        }
    }
}
