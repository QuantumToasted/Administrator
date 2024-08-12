using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Models;

namespace Administrator.Bot;

public sealed class LuaMessage(IMessage message, Snowflake guildId, DiscordLuaLibraryBase library) : ILuaModel<LuaMessage>
{
    public long Id { get; } = (long)message.Id.RawValue;

    public LuaChannel Channel { get; } = CreateChannel(message, guildId, library);

    public LuaUser Author { get; } = CreateUser(message.Author, library);

    public string Content { get; } = message.Content;

    public LuaEmbed[] Embeds { get; } = GenerateEmbeds(message, library);

    public LuaAttachment[] Attachments { get; } = GenerateAttachments(message, library);
    
    private static LuaChannel CreateChannel(IMessage message, Snowflake guildId, DiscordLuaLibraryBase library)
    {
        var bot = (DiscordBotBase)message.Client;

        return bot.GetChannel(guildId, message.ChannelId) switch
        {
            ITextChannel textChannel => new LuaTextChannel(textChannel, library),
            IVoiceChannel voiceChannel => new LuaVoiceChannel(voiceChannel, library),
            ICategoryChannel categoryChannel => new LuaCategoryChannel(categoryChannel, library),
            IThreadChannel threadChannel => new LuaThreadChannel(threadChannel, library),
            _ => new LuaUnknownChannel(new TransientUnknownGuildChannel(bot, new ChannelJsonModel { Id = message.ChannelId, GuildId = guildId }))
        };
    }

    private static LuaUser CreateUser(IUser user, DiscordLuaLibraryBase library)
    {
        return user is IMember member
            ? new LuaMember(member, library)
            : new LuaUser(user);
    }

    private static LuaEmbed[] GenerateEmbeds(IMessage message, DiscordLuaLibraryBase library)
        => message is IUserMessage { Embeds: { Count: > 0 } embeds } ? embeds.Select(x => new LuaEmbed(x)).ToArray() : [];

    private static LuaAttachment[] GenerateAttachments(IMessage message, DiscordLuaLibraryBase library)
        => message is IUserMessage { Attachments: { Count: > 0 } attachments } ? attachments.Select(x => new LuaAttachment(x)).ToArray() : [];
}