using ServerManagerTool.Enums;
using System.Collections.Generic;

namespace ServerManagerTool.Interface
{
    public interface ISpawnIniValuesCollection
    {
        IEnumerable<string> ToIniValues(NPCSpawnContainerType containerType);
    }
}
