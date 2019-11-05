using System.Text;
using Humanizer;
using Qmmands;

namespace Administrator.Extensions
{
    public static class CommandExtensions
    {
        public static string FormatArguments(this Command command)
        {
            var builder = new StringBuilder();
            foreach (var parameter in command.Parameters)
            {
                builder.Append(' ')
                    .Append(parameter.IsOptional ? '[' : '<')
                    .Append(parameter.Name.Humanize().ToLower()) // TODO: possible localized parameters
                    .Append(parameter.IsRemainder ? "..." : string.Empty)
                    .Append(parameter.IsOptional ? ']' : '>')
                    .Append(parameter.IsMultiple ? "[]" : string.Empty);
            }

            return builder.ToString();
        }
    }
}