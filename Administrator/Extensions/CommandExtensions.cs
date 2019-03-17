using System.Text;
using Qmmands;

namespace Administrator.Extensions
{
    public static class CommandExtensions
    {
        public static string Format(this Command command)
        {
            var builder = new StringBuilder(command.FullAliases[0]);
            foreach (var parameter in command.Parameters)
            {
                builder.Append(' ')
                    .Append(parameter.IsOptional ? '[' : '<')
                    .Append(parameter.Name)
                    .Append(parameter.IsRemainder ? "..." : string.Empty)
                    .Append(parameter.IsOptional ? ']' : '>')
                    .Append(parameter.IsMultiple ? "[]" : string.Empty);
            }

            return builder.ToString();
        }
    }
}