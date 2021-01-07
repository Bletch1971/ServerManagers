using System.Collections.Generic;

namespace ServerManagerTool.Common.Interfaces
{
    public interface IIniValuesList
    {
        IEnumerable<string> ToIniValues(object excludeIfValue);
    }
}
