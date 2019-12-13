using System.Threading.Tasks;
using Administrator.Common;
using CommandLine;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class MassPunishmentParser<TPunishment> : TypeParser<TPunishment>
        where TPunishment : MassPunishment
    {
        private static readonly Parser ArgParser = new Parser(x =>
        {
            x.AutoHelp = false;
            x.HelpWriter = null;
            x.AutoVersion = false;
        });

        public override ValueTask<TypeParserResult<TPunishment>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;

            TPunishment punishment = default;
            ArgParser.ParseArguments<TPunishment>(value.Split(' '))
                .WithParsed(x => punishment = x)
                .WithNotParsed(_ => punishment = default);

            return punishment is { }
                ? TypeParserResult<TPunishment>.Successful(punishment)
                : TypeParserResult<TPunishment>.Unsuccessful(context.Localize("masspunishmentparser_errors"));
        }
    }
}