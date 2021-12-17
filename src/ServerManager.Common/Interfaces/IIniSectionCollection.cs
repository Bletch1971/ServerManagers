using System.Collections.Generic;

namespace ServerManagerTool.Common.Interfaces
{
    public interface IIniSectionCollection
    {
        IIniValuesCollection[] Sections { get; }

        void Add(string sectionName, IEnumerable<string> values);

        void Update();
    }
}
