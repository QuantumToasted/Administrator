using Administrator.Extensions;
using Administrator.Services;
using Discord;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Qmmands;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

            var msg = await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("owner_eval_inprogress"))
                .WithDescription($"```cs\n{script}\n```")
                .Build());
            
            try
            {
                var watch = Stopwatch.StartNew();
                var sopts = ScriptOptions.Default
                    .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text",
                        "System.Threading.Tasks", "Discord", "Discord.WebSocket", "Administrator.Extensions",
                        "Administrator.Database", "Humanizer")
                    .WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                        .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));

                var result =
                    await CSharpScript.EvaluateAsync(script, sopts, new Globals { Context = Context });

                await msg.ModifyAsync(x => x.Embed = new EmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle(Localize("owner_eval_complete"))
                    .WithDescription(Format.Code(script, "cs"))
                    .AddField(Localize("owner_eval_return"), result is { }
                        ? Format.Code(result.GetType().ToString()) + '\n' + result.ToString()
                        : Localize("info_none"), true)
                    .AddField(Localize("owner_eval_executiontime"), $"{watch.ElapsedMilliseconds / 1000D:F}s", true)
                    .Build());
            }
            catch (CompilationErrorException ex)
            {
                await msg.ModifyAsync(x => x.Embed = new EmbedBuilder()
                    .WithErrorColor()
                    .WithTitle(Localize("owner_eval_failed"))
                    .AddField(Localize("owner_eval_errors"), 
                        Format.Code(string.Join('\n', ex.Diagnostics.Select(x => x.GetMessage())), ""))
                    .Build());
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
