using System.Security.Cryptography;
using System.Text;
using Administrator.Database;
using Qommon;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool HasSetting(this Guild guild, GuildSettings setting)
        => guild.Settings.HasFlag(setting);
}