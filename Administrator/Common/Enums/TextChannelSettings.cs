using System;

namespace Administrator.Common
{
    [Flags]
    public enum TextChannelSettings
    {
        SendCommandErrors = 1,
        DeleteCommandMessages = 2
    }
}