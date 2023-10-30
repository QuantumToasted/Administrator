using Disqord;
using Disqord.Rest;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public sealed partial class DiscordRestLuaLibrary
{
    private partial IEnumerable<string> RegisterRoleRestMethods(Lua lua)
    {
        yield return lua.SetStringGlobal("createRole", (Func<string, int?, LuaTable>)CreateRole);
        yield return lua.SetStringGlobal("modifyRoleName", (Action<long, string>)ModifyRoleName);
        yield return lua.SetStringGlobal("modifyRoleColor", (Action<long, int?>)ModifyRoleColor);
    }

    private LuaTable CreateRole(string name, int? color = null)
    {
        Guard.IsNotNullOrWhiteSpace(name);

        return RunWait(async () =>
        {
            var role = await bot.CreateRoleAsync(guildId, x =>
            {
                x.Name = name;

                if (color.HasValue)
                    x.Color = new Color(color.Value);
            });

            return _lua.ConvertEntity(role);
        });
    }

    private void ModifyRoleName(long roleId, string name)
    {
        RunWait(() => bot.ModifyRoleAsync(guildId, (ulong)roleId, x => x.Name = name));
    }

    private void ModifyRoleColor(long roleId, int? color)
    {
        RunWait(() => bot.ModifyRoleAsync(guildId, (ulong)roleId, x => x.Color = color.HasValue ? new Color(color.Value) : null));
    }
}