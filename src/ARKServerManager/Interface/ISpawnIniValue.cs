using ServerManagerTool.Enums;

namespace ServerManagerTool.Interface
{
    public interface ISpawnIniValue
    {
        string ToIniValue(NPCSpawnContainerType containerType);
    }
}
