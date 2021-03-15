using System.Text;
using Qmmands;

namespace Administrator.Extensions
{
    public static class CommandExtensions
    {
        public static Module GetTopLevelModule(this Module module)
        {
            while (module.Parent is not null)
            {
                module = module.Parent;
            }

            return module;
        }
        
        public static string FormatArguments(this Command command)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < command.Parameters.Count; i++)
            {
                var parameter = command.Parameters[i];
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