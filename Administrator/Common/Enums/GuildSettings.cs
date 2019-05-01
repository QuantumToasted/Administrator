using System;

namespace Administrator.Common
{
    [Flags]
    public enum GuildSettings
    {
        Punishments = 1,
        Modmail = 2,
        AutoPunishments = 4
    }
}