using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public sealed class EmojiTypeParser : DiscordGuildTypeParser<IEmoji>
{
    private EmojiService? _emojiService;

    public override ValueTask<ITypeParserResult<IEmoji>> ParseAsync(IDiscordGuildCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        _emojiService ??= context.Services.GetRequiredService<EmojiService>();

        var str = value.ToString();

        if (_emojiService.TryParseEmoji(str, out var emoji))
        {
            if (emoji is ICustomEmoji customEmoji && 
                context.Bot.GetGuild(context.GuildId) is {Emojis.Count: > 0} guild &&
                guild.Emojis.TryGetValue(customEmoji.Id, out var guildEmoji))
            {
                return Success(Optional.Create((IEmoji) guildEmoji));
            }

            return Success(Optional.Create(emoji));
        }

        return Failure($"The input string \"{str}\" was not a valid emoji string.");
    }
}