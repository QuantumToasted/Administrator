using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Laylua;
using Microsoft.Extensions.DependencyInjection;
using Qommon;

namespace Administrator.Bot;

public sealed class LuaCommandContext(IDiscordApplicationGuildCommandContext context, Lua lua, DiscordLuaLibraryBase library) : ILuaModel<LuaCommandContext>
{
    public string[] Path { get; } = SlashCommandMentionService.GetPath(context.Command!)!.Split(' ');
    
    public LuaMember Author { get; } = new(context.Author, library);

    public LuaGuildChannel? Channel { get; } = context.Bot.GetChannel(context.GuildId, context.ChannelId) switch
    {
        ITextChannel textChannel => new LuaTextChannel(textChannel, library),
        IVoiceChannel voiceChannel => new LuaVoiceChannel(voiceChannel, library),
        ICategoryChannel categoryChannel => new LuaCategoryChannel(categoryChannel, library),
        IThreadChannel threadChannel => new LuaThreadChannel(threadChannel, library),
        _ => null
    };

    public LuaGuild? Guild { get; } = context.Bot.GetGuild(context.GuildId) is { } guild ? new LuaGuild(guild, library) : null;

    public LuaTable? Parameters { get; } = GenerateParameters(context, lua, library);

    public void Reply(string text)
    {
        Guard.IsNotNullOrWhiteSpace(text);
        library.RunWait(ct => context.Interaction.RespondOrFollowupAsync(new LocalInteractionMessageResponse().WithContent(text), ct));
    }

    public void Reply(LuaTable msg)
    {
        Guard.IsNotNull(msg);
        var message = DiscordLuaLibraryBase.ConvertMessage<LocalInteractionMessageResponse>(msg);
        library.RunWait(ct => context.Interaction.RespondOrFollowupAsync(message, ct));
    }

    private static LuaTable? GenerateParameters(IDiscordApplicationCommandContext context, Lua lua, DiscordLuaLibraryBase library)
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
                        SlashCommandOptionType.String => (string) option.Value!,
                        SlashCommandOptionType.Integer => (long) option.Value!,
                        SlashCommandOptionType.Boolean => (bool) option.Value!,
                        SlashCommandOptionType.User when 
                            interaction.Entities.Users.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var user) => user is IMember member
                                ? new LuaMember(member, library)
                                : new LuaUser(user),
                        SlashCommandOptionType.User => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Channel when 
                            interaction.Entities.Channels.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var channel) => 
                            GetChannel(context.Bot, context.GuildId!.Value, channel, library),
                        SlashCommandOptionType.Channel => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Role when 
                            interaction.Entities.Roles.TryGetValue(ulong.Parse(option.Value!.ToString()!), out var role) => new LuaRole(role),
                        SlashCommandOptionType.Role => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Mentionable => ulong.Parse(option.Value!.ToString()!),
                        SlashCommandOptionType.Number => (double) option.Value!,
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

        return null;
        
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

        static LuaChannel GetChannel(DiscordBotBase bot, Snowflake guildId, IChannel channel, DiscordLuaLibraryBase library)
        {
            var c = bot.GetChannel(guildId, channel.Id);

            IThreadChannel? thread;
            try
            {
                thread = channel.Type is ChannelType.NewsThread or ChannelType.PublicThread or ChannelType.PrivateThread
                    ? library.RunWait<IThreadChannel?>(async ct =>
                    {
                        var t = await bot.FetchChannelAsync(channel.Id, cancellationToken: ct);
                        return t as IThreadChannel;
                    })
                    : null;
            }
            catch
            {
                thread = null;
            }
            
            return channel.Type switch
            {
                ChannelType.Text when c is ITextChannel textChannel => new LuaTextChannel(textChannel, library),
                ChannelType.Voice when c is IVoiceChannel voiceChannel => new LuaVoiceChannel(voiceChannel, library),
                ChannelType.Category when c is ICategoryChannel categoryChannel => new LuaCategoryChannel(categoryChannel, library),
                ChannelType.News when c is ITextChannel textChannel => new LuaTextChannel(textChannel, library),
                ChannelType.NewsThread when thread is not null => new LuaThreadChannel(thread, library),
                ChannelType.PublicThread when thread is not null => new LuaThreadChannel(thread, library),
                ChannelType.PrivateThread when thread is not null => new LuaThreadChannel(thread, library),
                _ => new LuaUnknownChannel(channel)
            };
        }
    }
}