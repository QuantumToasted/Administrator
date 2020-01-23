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
using Administrator.Common;
using Administrator.Common.LocalizedEmbed;
using Administrator.Database;
using Administrator.Extensions;
using Disqord;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Commands
{
    [Name("Owner")]
    [RequireOwner]
    public class OwnerCommands : AdminModuleBase
    {
        public CancellationTokenSource TokenSource { get; set; }

        public LoggingService Logging { get; set; }

        public PaginationService Pagination { get; set; }

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

        [Group("status")]
        public sealed class StatusCommands : OwnerCommands
        {
            [Command("list")]
            public async ValueTask<AdminCommandResult> ListAsync()
            {
                var statuses = await Context.Database.Statuses
                    .OrderBy(x => x.Id)
                    .ToListAsync();

                if (statuses.Count == 0)
                    return CommandErrorLocalized("owner_statuses_none");

                var pages = DefaultPaginator.GeneratePages(statuses, 10, status => new LocalEmbedFieldBuilder()
                        .WithName($"{Localize("info_id")}: {status.Id}")
                        .WithValue($"{Markdown.Bold(status.Type.ToString())} {status.Text}"),
                    builderFunc: () => new LocalizedEmbedBuilder(this)
                        .WithSuccessColor()
                        .WithLocalizedTitle("owner_statuses_title"));

                if (pages.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                    return CommandSuccess();
                }

                return CommandSuccess(embed: pages[0].Embed);
            }

            [Command("add")]
            public async ValueTask<AdminCommandResult> AddAsync(ActivityType type, 
                [Remainder, Replace("\n", "")] string text)
            {
                if (type == ActivityType.Custom)
                    type = ActivityType.Playing;

                var status = Context.Database.Statuses.Add(new CyclingStatus(type, text)).Entity;
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("owner_status_add", args: Markdown.Code($"[#{status.Id}]"));
            }

            [Command("remove")]
            public async ValueTask<AdminCommandResult> RemoveAsync([MustBe(Operator.GreaterThan, 0)] int id)
            {
                if (!(await Context.Database.Statuses.FindAsync(id) is { } status))
                    return CommandErrorLocalized("owner_status_remove_notfound");

                Context.Database.Statuses.Remove(status);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized("owner_status_remove");
            }

            [Command("set")]
            public async ValueTask<AdminCommandResult> SetAsync([MustBe(Operator.GreaterThan, 0)] int id)
            {
                if (!(await Context.Database.Statuses.FindAsync(id) is { } status))
                    return CommandErrorLocalized("owner_status_remove_notfound");

                await Context.Client.SetPresenceAsync(new LocalActivity(status.Text, status.Type));
                await Context.Message.AddReactionAsync(EmojiTools.Checkmark);
                return CommandSuccess();
            }
        }
        
        public class Globals
        {
            public AdminCommandContext Context { get; set; }
        }
    }
}
