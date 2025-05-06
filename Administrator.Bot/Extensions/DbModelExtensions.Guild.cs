using System.Security.Cryptography;
using System.Text;
using Administrator.Core;
using Administrator.Database;
using Qommon;

namespace Administrator.Bot;

public static partial class DbModelExtensions
{
    public static bool HasSetting(this GuildConfiguration guild, GuildSettings setting)
        => guild.Settings.HasFlag(setting);
}