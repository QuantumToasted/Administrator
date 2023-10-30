using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Interaction;
using Laylua;
using Qmmands;
using Qommon;
using Qommon.Collections;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(2);
    
    public static ApplicationModuleBuilder ToApplicationModule(this LuaCommand luaCommand, DiscordBotBase bot, ApplicationModuleBuilder parent)
    {
        using var lua = new Lua();

        lua.OpenLibrary(new DiscordEnumLuaLibrary(bot));

        using var metadata = lua.Evaluate<LuaTable>($"return {Encoding.Unicode.GetString(luaCommand.Metadata)}");
        Guard.IsNotNull(metadata);

        var commandDescription = metadata.GetValueOrDefault<string, string>("description") ?? "No description.";
        Guard.IsNotNullOrWhiteSpace(commandDescription);
        Guard.HasSizeLessThanOrEqualTo(commandDescription, Discord.Limits.ApplicationCommand.MaxDescriptionLength);
        
        var module = new ApplicationModuleBuilder(parent)
        {
            // Alias = Name,
            //Description = $"Lua command {Name} for guild {GuildId}"
            Description = commandDescription
        };

        if (metadata.TryGetValue<string, long>("permissions", out var requiredPermissions))
        {
            module.Checks.Add(new RequireInitialAuthorPermissionsAttribute((Permissions) (ulong) requiredPermissions));
        }

        if (metadata.TryGetValue<string, LuaTable>("options", out var options))
        {
            PopulateParameters(module, options);
        }

        return module;

        void PopulateParameters(ApplicationModuleBuilder currentModule, LuaTable opts)
        {
            foreach (var (key, value) in opts)
            {
                Guard.IsNotNull(key.Value);
                var optionName = Guard.IsOfType<string>(key.Value);
                Guard.IsNotNullOrWhiteSpace(optionName);
                Guard.HasSizeLessThanOrEqualTo(optionName, Discord.Limits.ApplicationCommand.Option.MaxNameLength);
                
                Guard.IsNotNull(value.Value);
                var optionMetadata = Guard.IsOfType<LuaTable>(value.Value);

                var rawOptionType = optionMetadata.GetValueOrDefault<string, string>("type") ?? "string";
                var optionType = Enum.Parse<SlashCommandOptionType>(rawOptionType, true);

                if (optionType is SlashCommandOptionType.SubcommandGroup)
                {
                    var subModule = new ApplicationModuleBuilder(currentModule)
                    {
                        Alias = optionName/*,
                        Description = optionMetadata.GetValueOrDefault<string, string>("description") ?? "No description."*/
                    };
                    
                    currentModule.Submodules.Add(subModule);
                    
                    var subOptions = optionMetadata.GetValue<string, LuaTable>("options");
                    PopulateParameters(subModule, subOptions);
                    
                    continue;
                }

                var command = new ApplicationCommandBuilder(currentModule, new DelegateCommandCallback(luaCommand.ExecuteAsync));
                if (optionType is SlashCommandOptionType.Subcommand)
                {
                    command.Alias = optionName;
                    command.Description = optionMetadata.GetValueOrDefault<string, string>("description") ?? "No description.";
                    command.Parameters.AddRange(EnumerateParameters(command, optionMetadata.GetValue<string, LuaTable>("options")));
                }
                else
                {
                    command.Alias = luaCommand.Name;
                    command.Description = commandDescription;
                    command.Parameters.AddRange(EnumerateParameters(command, opts));
                }

                currentModule.Commands.Add(command);
            }

            static IEnumerable<ApplicationParameterBuilder> EnumerateParameters(ApplicationCommandBuilder command, LuaTable options)
            {
                foreach (var (key, value) in options)
                {
                    var optionName = key.GetValue<string>();
                    Guard.IsNotNullOrWhiteSpace(optionName);

                    var optionMetadata = value.GetValue<LuaTable>();
                    Guard.IsNotNull(optionMetadata);

                    var rawOptionType = optionMetadata.GetValueOrDefault<string, string>("type") ?? "string";
                    var optionType = Enum.Parse<SlashCommandOptionType>(rawOptionType, true);

                    var type = optionType switch
                    {
                        SlashCommandOptionType.String => typeof(string),
                        SlashCommandOptionType.Integer => typeof(int),
                        SlashCommandOptionType.Boolean => typeof(bool),
                        SlashCommandOptionType.User => typeof(IUser),
                        SlashCommandOptionType.Channel => typeof(IChannel),
                        SlashCommandOptionType.Role => typeof(IRole),
                        SlashCommandOptionType.Number => typeof(double),
                        SlashCommandOptionType.Attachment => typeof(IAttachment),
                        _ => throw new ArgumentOutOfRangeException(nameof(optionType), $"Invalid option type {optionType}")
                    };

                    var parameter = new ApplicationParameterBuilder(command, type)
                    {
                        Name = optionName,
                        Description = optionMetadata.GetValueOrDefault<string, string>("description") ?? "No description."
                    };
                    
                    switch (optionType)
                    {
                        case SlashCommandOptionType.Channel when optionMetadata.TryGetValue<string, LuaTable>("channelTypes", out var channelTypeMetadata):
                        {
                            var channelTypes = new HashSet<ChannelType>();
                            foreach (var channelTypePair in channelTypeMetadata)
                            {
                                Guard.IsNotNull(channelTypePair.Value.Value);
                                var channelType = Enum.Parse<ChannelType>(Guard.IsOfType<string>(channelTypePair.Value.Value), true);
                                channelTypes.Add(channelType);
                            }
                        
                            parameter.CustomAttributes.Add(new ChannelTypesAttribute(channelTypes.ToArray()));
                            break;
                        }
                        case SlashCommandOptionType.Number or SlashCommandOptionType.Integer or SlashCommandOptionType.String:
                        {
                            if (optionMetadata.TryGetValue<string, double>("maximum", out var maximumValue))
                                parameter.Checks.Add(new MaximumAttribute(maximumValue));
                        
                            if (optionMetadata.TryGetValue<string, double>("minimum", out var minimumValue))
                                parameter.Checks.Add(new MaximumAttribute(minimumValue));
                            
                            break;
                        }
                    }

                    yield return parameter;
                }
            }
        }
    }

    public static async ValueTask<IResult?> ExecuteAsync(this LuaCommand luaCommand, ICommandContext context)
    {
        var interactionContext = Guard.IsAssignableToType<IDiscordApplicationGuildCommandContext>(context);

        using var cts = new CancellationTokenSource(CommandTimeout);
        using var lua = new Lua();
        lua.OpenDiscordLibraries(interactionContext);
        lua.OpenLibrary(LuaLibraries.Standard.Math);

        IResult? result = null;
        try
        {
            var code = Encoding.Unicode.GetString(luaCommand.Command);
            
            await Task.Run(() =>
            {
                var response = lua.Evaluate<object>(code);
                result = response switch
                {
                    LuaTable msg => new DiscordInteractionResponseCommandResult(interactionContext,
                        DiscordLuaLibraryBase.ConvertMessage<LocalInteractionMessageResponse>(msg)),
                    string content => new DiscordInteractionResponseCommandResult(interactionContext,
                        new LocalInteractionMessageResponse().WithContent(content)),
                    _ => Results.Success
                };
            }, cts.Token);
        }
        catch (TaskCanceledException)
        {
            result = Results.Failure("Command timeout exceeded.");
        }
        catch (Exception ex)
        {
            result = Results.Exception("LuaCommandExecute", ex);
        }

        return result;
    }
}