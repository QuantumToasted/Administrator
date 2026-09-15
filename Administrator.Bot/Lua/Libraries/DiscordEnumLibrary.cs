using Disqord;
using Humanizer;
using Laylua;

namespace Administrator.Bot;

public sealed class DiscordEnumLibrary(EmojiService emojiService) : DiscordLuaLibrary
{
    public override string Name => "enum";
    
    protected override IEnumerable<string> EnumerateGlobals(Lua lua)
    {
        using (var permissions = lua.CreateTable())
        {
            foreach (var flag in Enum.GetValues<Permissions>())
            {
                permissions.SetValue(flag.Humanize(LetterCasing.AllCaps).Replace(' ', '_'), (long) flag);
            }
            
            yield return lua.SetStringGlobal("Permission", permissions);
        }

        using (var emojis = lua.CreateTable())
        {
            foreach (var (name, emoji) in emojiService.Names)
            {
                emojis.SetValue(name.ToUpper(), emoji.Surrogates);
            }
            
            yield return lua.SetStringGlobal("Emoji", emojis);
        }
        
        using (var styles = lua.CreateTable())
        {
            foreach (var flag in Enum.GetValues<LocalButtonComponentStyle>())
            {
                styles.SetValue(flag.Humanize(LetterCasing.AllCaps).Replace(' ', '_'), (long) flag);
            }
            
            yield return lua.SetStringGlobal("ButtonStyle", styles);
        }
    }
}