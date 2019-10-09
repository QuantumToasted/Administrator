using System;

namespace Administrator.Common
{
    [Flags]
    public enum LevelUpNotification
    {
        None = 0,
        Reaction = 1,
        Channel = 2,
        DM = 4
    }
}