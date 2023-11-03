using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Laylua;

namespace Administrator.Bot;

public sealed class DiscordCommandContextLibrary(IDiscordApplicationGuildCommandContext context) : DiscordLuaLibraryBase
{
    public override string Name => "context";

    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        var discordChannel = context.Bot.GetChannel(context.GuildId, context.ChannelId)!;
        var discordGuild = context.Bot.GetGuild(context.GuildId)!;
        
        using (var ctx = lua.CreateTable())
        {
            using (var author = lua.ConvertEntity(context.Author))
            {
                ctx.SetValue("author", author);
            }

            using (var channel = lua.ConvertEntity(discordChannel))
            {
                ctx.SetValue("channel", channel);
            }

            using (var guild = lua.ConvertEntity(discordGuild))
            {
                ctx.SetValue("guild", guild);
            }
            
            using (var command = lua.CreateTable())
            {
                command.SetValue("name", context.Interaction.CommandName);
                command.SetValue("id", (long) (ulong) context.Interaction.CommandId);

                ctx.SetValue("command", command);
            }

            if (context.Interaction is ISlashCommandInteraction { Options: { Count: > 0 } rawOptions } interaction)
            {
                var options = GetOptionsWithValues(rawOptions);

                if (options.Count > 0)
                {
                    using (var parameters = lua.CreateTable())
                    {
                        foreach (var option in options)
                        {
                            object value = option.Type switch
                            {
                                SlashCommandOptionType.String => (string) option.Value!,
                                SlashCommandOptionType.Integer => (long) option.Value!,
                                SlashCommandOptionType.Boolean => (bool) option.Value!,
                                SlashCommandOptionType.User when 
                                    interaction.Entities.Users.TryGetValue((ulong) option.Value!, out var user) => lua.ConvertEntity(user),
                                SlashCommandOptionType.User => (long) option.Value!,
                                SlashCommandOptionType.Channel when 
                                    interaction.Entities.Channels.TryGetValue((ulong) option.Value!, out var channel) => lua.ConvertEntity(channel),
                                SlashCommandOptionType.Channel => (long) option.Value!,
                                SlashCommandOptionType.Role when 
                                    interaction.Entities.Roles.TryGetValue((ulong) option.Value!, out var role) => lua.ConvertEntity(role),
                                SlashCommandOptionType.Role => (long) option.Value!,
                                SlashCommandOptionType.Mentionable => (long) option.Value!,
                                SlashCommandOptionType.Number => (double) option.Value!,
                                SlashCommandOptionType.Attachment when 
                                    interaction.Entities.Attachments.TryGetValue((ulong) option.Value!, out var attachment) => attachment.Url,
                                SlashCommandOptionType.Attachment => (long) option.Value!,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                            
                            parameters.SetValue(option.Name, value);
                        }


                        ctx.SetValue("parameters", parameters);
                    }
                }
            }
            
            yield return lua.SetStringGlobal("ctx", ctx);
        }
        
        yield return lua.SetStringGlobal("reply", (Action<LuaTable>) Reply);

        static IList<ISlashCommandInteractionOption> GetOptionsWithValues(IReadOnlyDictionary<string, ISlashCommandInteractionOption> options)
        {
            var list = new List<ISlashCommandInteractionOption>();

            foreach (var (_, option) in options)
            {
                if (option.Type is SlashCommandOptionType.SubcommandGroup or SlashCommandOptionType.Subcommand && option.Options.Count > 0)
                {
                    list.AddRange(GetOptionsWithValues(option.Options));
                    continue;
                }
                
                if (option.Value is null)
                {
                    continue;
                }
                
                list.Add(option);
            }

            return list;
        }
    }
    
    private void Reply(LuaTable msg)
    {
        var message = ConvertMessage<LocalInteractionFollowup>(msg);
        RunWait(() => context.Interaction.Followup().SendAsync(message));
    }
}