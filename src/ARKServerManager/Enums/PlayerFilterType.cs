using System;

namespace ServerManagerTool.Enums
{
    [Flags]
    public enum PlayerFilterType
    {
        None = 0,
        Offline = 0x1,
        Online = 0x2,
        Whitelisted = 0x8,
        Invalid = 0x10,
        Admin = 0x20,
    }
}
