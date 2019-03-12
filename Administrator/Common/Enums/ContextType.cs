using System;

namespace Administrator.Common
{
    [Flags]
    public enum ContextType
    {
        Guild = 1,
        DM = 2
    }
}