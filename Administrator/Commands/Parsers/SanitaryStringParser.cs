using System.Linq;
using System.Threading.Tasks;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class SanitaryStringParser : TypeParser<string>
    {
        public override ValueTask<TypeParserResult<string>> ParseAsync(Parameter parameter, string value, CommandContext context)
        {
            var attribute = parameter.Attributes.OfType<SanitaryAttribute>().First();
            return TypeParserResult<string>.Successful(attribute.Transformation(value));
        }
    }
}