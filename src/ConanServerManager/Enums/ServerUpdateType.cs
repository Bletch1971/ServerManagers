using System;

namespace ServerManagerTool.Enums
{
    [Flags]
    public enum ServerUpdateType
    {
        None = 0,
        Server = 1,
        Mods = 2,
        ServerAndMods = Server | Mods,
    }
}
