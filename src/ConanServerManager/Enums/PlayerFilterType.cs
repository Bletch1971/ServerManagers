using System;

namespace ServerManagerTool.Enums
{
    [Flags]
    public enum PlayerFilterType
    {
        None = 0,
        Offline = 0x1,
        Online = 0x2,
        Admin = 0x4,
        Invalid = 0x10,
        Whitelisted = 0x20,
    }
}
