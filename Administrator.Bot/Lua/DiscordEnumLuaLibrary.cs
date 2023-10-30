using Disqord;
using Disqord.Bot;
using Humanizer;
using Laylua;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public sealed class DiscordEnumLuaLibrary(DiscordBotBase bot) : DiscordLuaLibraryBase
{
    private readonly EmojiService _emojiService = bot.Services.GetRequiredService<EmojiService>();

    public override string Name => "enums";

    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        using (var permissions = lua.CreateTable())
        {
            foreach (var flag in Enum.GetValues<Permissions>())
            {
                permissions.SetValue(flag.Humanize(LetterCasing.AllCaps).Replace(' ', '_'), (long) (ulong) flag);
            }
            
            yield return lua.SetStringGlobal("Permission", permissions);
        }

        using (var emojis = lua.CreateTable())
        {
            foreach (var (name, emoji) in _emojiService.Names)
            {
                emojis.SetValue(name.ToUpper(), emoji.Surrogates);
            }
            
            yield return lua.SetStringGlobal("Emoji", emojis);
        }
    }
}