using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Laylua;
using Laylua.Marshaling;

namespace Administrator.Bot;

[LuaType]
public sealed partial class LuaCommandContext(IDiscordApplicationGuildCommandContext context, Lua lua)
{
    public string[] Path { get; } = SlashCommandMentionService.GetPath(context.Command!)!.Split(' ');
    
    public LuaMember Author { get; } = new(context.Author);

    public LuaGuildChannel? Channel { get; } = context.Bot.GetChannel(context.GuildId, context.ChannelId) switch
    {
        ITextChannel textChannel => new LuaTextChannel(textChannel),
        IVoiceChannel voiceChannel => new LuaVoiceChannel(voiceChannel),
        ICategoryChannel categoryChannel => new LuaCategoryChannel(categoryChannel),
        IThreadChannel threadChannel => new LuaThreadChannel(threadChannel),
        _ => null
    };

    public LuaGuild? Guild { get; } = context.Bot.GetGuild(context.GuildId) is { } guild ? new LuaGuild(guild) : null;

    // TODO: reduce dependencies on LuaTable
    public LuaTable? Parameters { get; } = GenerateParameters(context, lua);

    public async Task<bool> Reply(LuaMessage msg)
    {
        try
        {
            var message = LocalInteractionMessageResponse.CreateFrom(msg);
            await context.Interaction.RespondOrFollowupAsync(message);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> ReplyEphemeral(LuaMessage msg)
    {
        try
        {
            var message = LocalInteractionMessageResponse.CreateFrom(msg);
            await context.Interaction.RespondOrFollowupAsync(message.WithIsEphemeral());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static LuaTable GenerateParameters(IDiscordApplicationCommandContext context, Lua lua)
    {
        if (context.Interaction is ISlashCommandInteraction { Options: { Count: > 0 } rawOptions } interaction)
        {
            var options = GetOptionsWithValues(rawOptions);

            if (options.Count > 0)
            {
                var parameters = lua.CreateTable();
                
                foreach (var option in options)
                {
                    object value = option.Type switch
                    {
                        SlashCommandOptionType.String => option.Value!.ToType<string>()!,
                        SlashCommandOptionType.Integer => option.Value!.ToType<long>(),
                        SlashCommandOptionType.Boolean => option.Value!.ToType<bool>(),
                        SlashCommandOptionType.User when 
                            interaction.Entities.Users.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var user) => 
                                LuaUser.FromUser(user),
                        SlashCommandOptionType.User => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Channel when 
                            interaction.Entities.Channels.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var channel) => 
                            GetChannel(context.Bot, context.GuildId!.Value, channel),
                        SlashCommandOptionType.Channel => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Role when 
                            interaction.Entities.Roles.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var role) => new LuaRole(role),
                        SlashCommandOptionType.Role => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Mentionable => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Number => option.Value!.ToType<double>(),
                        SlashCommandOptionType.Attachment when 
                            interaction.Entities.Attachments.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var attachment) => attachment.Url,
                        SlashCommandOptionType.Attachment => ulong.Parse(option.Value!.ToString()!),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                        
                    parameters.SetValue(option.Name, value);
                }

                return parameters;
            }
        }

        return lua.CreateTable();
        
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

        static LuaGuildChannel GetChannel(DiscordBotBase bot, Snowflake guildId, IChannel channel)
        {
            var c = bot.GetChannel(guildId, channel.Id);

            IThreadChannel? thread;
            try
            {
                thread = channel.Type is ChannelType.NewsThread or ChannelType.PublicThread or ChannelType.PrivateThread
                    ? bot.FetchChannelAsync(channel.Id).GetAwaiter().GetResult() as  IThreadChannel
                    : null;
            }
            catch
            {
                thread = null;
            }
            
            return channel.Type switch
            {
                ChannelType.Text when c is ITextChannel textChannel => new LuaTextChannel(textChannel),
                ChannelType.Voice when c is IVoiceChannel voiceChannel => new LuaVoiceChannel(voiceChannel),
                ChannelType.Category when c is ICategoryChannel categoryChannel => new LuaCategoryChannel(categoryChannel),
                ChannelType.News when c is ITextChannel textChannel => new LuaTextChannel(textChannel),
                ChannelType.NewsThread when thread is not null => new LuaThreadChannel(thread),
                ChannelType.PublicThread when thread is not null => new LuaThreadChannel(thread),
                ChannelType.PrivateThread when thread is not null => new LuaThreadChannel(thread),
                _ => null!
            };
        }
    }
}