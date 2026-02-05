using Administrator.Core;
using Administrator.Database;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool HasSetting(this GuildConfiguration guild, GuildSettings setting)
        => guild.Settings.HasFlag(setting);
}