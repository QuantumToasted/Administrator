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
            for (var i = 0; i < command.Parameters.Count; i++)
            {
                var parameter = command.Parameters[i];
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