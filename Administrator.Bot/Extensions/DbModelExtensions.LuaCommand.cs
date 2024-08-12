using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Disqord.Utilities.Threading;
using Laylua;
using Qmmands;
using Qommon;
using Qommon.Collections;
using Qommon.Metadata;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(2);
    private static readonly IReadOnlyDictionary<SlashCommandOptionType, Type> SlashCommandTypeMap = new Dictionary<SlashCommandOptionType, Type>
    {
        [SlashCommandOptionType.String] = typeof(string),
        [SlashCommandOptionType.Integer] = typeof(int),
        [SlashCommandOptionType.Boolean] = typeof(bool),
        [SlashCommandOptionType.User] = typeof(IUser),
        [SlashCommandOptionType.Channel] = typeof(IChannel),
        [SlashCommandOptionType.Role] = typeof(IRole),
        [SlashCommandOptionType.Number] = typeof(double),
        [SlashCommandOptionType.Attachment] = typeof(IAttachment)
    };

    public static void MutateApplicationModule(this LuaCommand luaCommand, DiscordBotBase bot, ApplicationModuleBuilder parentModule)
    {
        using var lua = new Lua();
        lua.OpenLibrary(new DiscordEnumLibrary(bot));

        var raw = Encoding.Default.GetString(luaCommand.Metadata.GZipDecompress());
        // metadata should always end with `return metadata`
        var table = lua.Evaluate<LuaTable>(raw);
        Guard.IsNotNull(table);
        var slashCommand = new LuaSlashCommand(table);
        
        Guard.IsNotNullOrWhiteSpace(luaCommand.Name);
        Guard.HasSizeLessThanOrEqualTo(luaCommand.Name, Discord.Limits.ApplicationCommand.MaxNameLength);

        Guard.IsNotNullOrWhiteSpace(slashCommand.Description);
        Guard.HasSizeLessThanOrEqualTo(slashCommand.Description, Discord.Limits.ApplicationCommand.MaxDescriptionLength);

        var options = slashCommand.GetOptions().ToList();
        if (options.Any(x => x.Type is SlashCommandOptionType.SubcommandGroup or SlashCommandOptionType.Subcommand))
        {
            var subModule = new ApplicationModuleBuilder(parentModule)
            {
                Alias = slashCommand.Name
            };
            
            parentModule.Submodules.Add(subModule);
            
            PopulateOptions(subModule, options);
            
            void PopulateOptions(ApplicationModuleBuilder currentModule, ICollection<LuaSlashCommandOption> opts)
            {
                foreach (var option in opts)
                {
                    Guard.IsNotNullOrWhiteSpace(option.Name);
                    Guard.HasSizeLessThanOrEqualTo(option.Name, Discord.Limits.ApplicationCommand.Option.MaxNameLength);
                    
                    if (option.Type is SlashCommandOptionType.SubcommandGroup)
                    {
                        var subCommandModule = new ApplicationModuleBuilder(currentModule)
                        {
                            Alias = option.Name
                        };
                        
                        currentModule.Submodules.Add(subCommandModule);

                        var subOpts = option.GetOptions().ToList();
                        Guard.IsNotEmpty(subOpts);
                        
                        PopulateOptions(subCommandModule, subOpts);
                        continue;
                    }

                    var command = new ApplicationCommandBuilder(currentModule, new DelegateCommandCallback(luaCommand.ExecuteAsync));
                    if (option.Type is SlashCommandOptionType.Subcommand)
                    {
                        Guard.IsNotNullOrWhiteSpace(option.Description);
                        Guard.HasSizeLessThanOrEqualTo(option.Description, Discord.Limits.ApplicationCommand.Option.MaxDescriptionLength);
                        
                        command.Alias = option.Name;
                        command.Description = option.Description;
                        
                        var requiredParameters = new List<ApplicationParameterBuilder>();
                        var optionalParameters = new List<ApplicationParameterBuilder>();

                        foreach (var parameter in EnumerateParameters(command, option.GetOptions()))
                        {
                            if (!parameter.DefaultValue.HasValue)
                            {
                                requiredParameters.Add(parameter);
                            }
                            else
                            {
                                optionalParameters.Add(parameter);
                            }
                        }

                        command.Parameters.AddRange(requiredParameters.Concat(optionalParameters));
                    }
                    else
                    {
                        command.Alias = luaCommand.Name;
                        command.Description = slashCommand.Description;
                        
                        var requiredParameters = new List<ApplicationParameterBuilder>();
                        var optionalParameters = new List<ApplicationParameterBuilder>();

                        foreach (var parameter in EnumerateParameters(command, options))
                        {
                            if (!parameter.DefaultValue.HasValue)
                            {
                                requiredParameters.Add(parameter);
                            }
                            else
                            {
                                optionalParameters.Add(parameter);
                            }
                        }

                        command.Parameters.AddRange(requiredParameters.Concat(optionalParameters));
                    }
                    
                    currentModule.Commands.Add(command);
                }
            }
        }
        else
        {
            var command = new ApplicationCommandBuilder(parentModule, new DelegateCommandCallback(luaCommand.ExecuteAsync))
            {
                Alias = luaCommand.Name,
                Description = slashCommand.Description
            };

            if (slashCommand.Permissions.HasValue)
            {
                command.Checks.Add(new RequireInitialAuthorPermissionsAttribute(slashCommand.Permissions.Value));
                command.Checks.Add(new RequireBotPermissionsAttribute(slashCommand.Permissions.Value));
                //command.Checks.Add(new RequireInitialAuthorPermissionsAttribute(slashCommand.Permissions.Value));
            }

            //var parameters = EnumerateParameters(command, options);

            var requiredParameters = new List<ApplicationParameterBuilder>();
            var optionalParameters = new List<ApplicationParameterBuilder>();

            foreach (var parameter in EnumerateParameters(command, options))
            {
                if (!parameter.DefaultValue.HasValue)
                {
                    requiredParameters.Add(parameter);
                }
                else
                {
                    optionalParameters.Add(parameter);
                }
            }
            
            command.Parameters.AddRange(requiredParameters.Concat(optionalParameters));
            
            parentModule.Commands.Add(command);
        }

        static IEnumerable<ApplicationParameterBuilder> EnumerateParameters(ApplicationCommandBuilder command, IEnumerable<LuaSlashCommandOption> options)
        {
            foreach (var option in options)
            {
                var type = SlashCommandTypeMap.GetValueOrDefault(option.Type) 
                           ?? throw new ArgumentOutOfRangeException(nameof(option.Type), $"Invalid option type {option.Type}");

                var parameter = new ApplicationParameterBuilder(command, type)
                {
                    Name = option.Name,
                    Description = option.Description,
                    DefaultValue = !option.IsRequired ? new Optional<object?>(default) : Optional<object?>.Empty
                };
                
                switch (option.Type)
                {
                    case SlashCommandOptionType.Channel when option.GetChannelTypes().ToHashSet() is { Count: > 0 } channelTypes:
                    {
                        parameter.CustomAttributes.Add(new ChannelTypesAttribute(channelTypes.ToArray()));
                        break;
                    }
                    case SlashCommandOptionType.Number or SlashCommandOptionType.Integer or SlashCommandOptionType.String:
                    {
                        if (option.Minimum.HasValue)
                            parameter.Checks.Add(new MinimumAttribute(option.Minimum.Value));
                        
                        if (option.Maximum.HasValue)
                            parameter.Checks.Add(new MaximumAttribute(option.Maximum.Value));

                        break;
                    }
                }

                yield return parameter;
                //command.Parameters.Add(parameter);
            }
        }
    }
    
    public static ValueTask<IResult?> ExecuteAsync(this LuaCommand luaCommand, ICommandContext context)
    {
        var interactionContext = Guard.IsAssignableToType<IDiscordApplicationGuildCommandContext>(context);
        interactionContext.SetMetadata("command", luaCommand.Name);

        using var lua = new Lua();
        using var cts = new Cts();
        lua.OpenLibrary(LuaLibraries.Standard.Math);
        lua.OpenLibrary(LuaLibraries.Standard.Base);
        lua.OpenLibrary(LuaLibraries.Standard.String);
        lua.OpenLibrary(LuaLibraries.Standard.Table);
        lua.OpenDiscordLibraries(interactionContext, cts.Token);

        IResult? result;

        try
        {
            var code = Encoding.Default.GetString(luaCommand.Command.GZipDecompress());
            cts.CancelAfter(CommandTimeout);
            
            object? response;
            try
            {
                response = lua.Evaluate<object>(code);
            }
            catch (InvalidOperationException) // The evaluation returned no results.
            {
                response = null;
            }
                
            result = response switch
            {
                LuaTable msg => new DiscordInteractionResponseCommandResult(interactionContext,
                    DiscordLuaLibraryBase.ConvertMessage<LocalInteractionMessageResponse>(msg).WithAllowedMentions(LocalAllowedMentions.None)),
                string content => new DiscordInteractionResponseCommandResult(interactionContext,
                    new LocalInteractionMessageResponse().WithContent(content).WithAllowedMentions(LocalAllowedMentions.None)),
                _ => Results.Success
            };
        }
        catch (Exception ex)
        {
            result = Results.Exception("executing a Lua command", ex);
        }

        return new(result);
    }
}