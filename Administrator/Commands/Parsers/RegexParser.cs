using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class RegexParser : TypeParser<Regex>
    {
        public override ValueTask<TypeParserResult<Regex>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
                
            Regex regex;
            try
            {
                regex = new Regex(value);
                return TypeParserResult<Regex>.Successful(regex);
            }
            catch (ArgumentException)
            {
                return TypeParserResult<Regex>.Unsuccessful(context.Localize("regexparser_invalid", value));
            }
        }
    }
}