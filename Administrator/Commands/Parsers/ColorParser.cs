using System;
using System.Globalization;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class ColorParser : TypeParser<Color>
    {
        public override ValueTask<TypeParserResult<Color>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext)ctx;

            // Parse hexadecimal color value
            value = value.TrimStart('#');
            if (int.TryParse(value, NumberStyles.HexNumber, default, out var result) && result <= 0xFFFFFF) // #FFFFFF
                return TypeParserResult<Color>.Successful(new Color(result));

            var values = value.Split(new[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);
            return values.Length == 3 &&
                   byte.TryParse(values[0], out var r) &&
                   byte.TryParse(values[1], out var g) &&
                   byte.TryParse(values[2], out var b)
                ? TypeParserResult<Color>.Successful(new Color(r, g, b))
                : TypeParserResult<Color>.Unsuccessful(context.Localize("colorparser_invalid"));
        }
    }
}