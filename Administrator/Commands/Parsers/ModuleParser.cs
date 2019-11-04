using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class ModuleParser : TypeParser<Module>
    {
        public override ValueTask<TypeParserResult<Module>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var commandService = context.ServiceProvider.GetRequiredService<CommandService>();

            var module =
                commandService.TopLevelModules.FirstOrDefault(x =>
                    x.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

            return module is { }
                ? TypeParserResult<Module>.Successful(module)
                : TypeParserResult<Module>.Unsuccessful(context.Localize("moduleparser_notfound"));
        }
    }
}