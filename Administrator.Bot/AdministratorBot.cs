using System.Reflection;
using System.Text;
using Administrator.Core;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Administrator.Bot;

public sealed partial class AdministratorBot(IOptions<DiscordBotConfiguration> options, ILogger<AdministratorBot> logger, IServiceProvider services,
    DiscordClient client) : DiscordBot(options, logger, services, client)
{
    protected override void MutateCommand(ICommandBuilder command)
    {
        // add slash command choices for [Flags] enums, too!
        foreach (var parameter in command.Parameters.Where(x => x.ReflectedType.IsEnum))
        {
            var enumType = parameter.ReflectedType;
            
            if (!enumType.GetCustomAttributes<FlagsAttribute>().Any())
            {
                LogSkippedEnumType(Logger, enumType);
                continue;
            }

            LogRegisteringEnumType(Logger, enumType);
            foreach (var value in Enum.GetValues(enumType))
            {
                var name = value.ToString()!;
                var valueMemberInfo = enumType.GetMember(name)[0];

                var nameBuilder = new StringBuilder(name);
                foreach (var attribute in valueMemberInfo.GetCustomAttributes())
                {
                    if (attribute is DescriptionAttribute { Description: var description })
                    {
                        nameBuilder.Append(" - ").Append(description);
                    }                    
                    else if (attribute is IgnoreEnumValueAttribute)
                    {
                        nameBuilder.Clear();
                    }
                }

                if (nameBuilder.Length > 0)
                {
                    parameter.CustomAttributes.Add(
                        new ChoiceAttribute(nameBuilder.ToString().Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength), name));
                }
            }
        }
        
        base.MutateCommand(command);
    }

    [LoggerMessage(LogLevel.Debug, "Skipping enum type {Type} because it doesn't have the [Flags] attribute.")]
    private static partial void LogSkippedEnumType(ILogger logger, Type type);

    [LoggerMessage(LogLevel.Information, "Registering slash command option choices for enum {Type}.")]
    private static partial void LogRegisteringEnumType(ILogger logger, Type type);
}