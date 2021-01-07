using System.Collections.Generic;

namespace ServerManagerTool.Common.Interfaces
{
    public interface IIniValuesCollection
    {
        string IniCollectionKey { get; }
        bool IsArray { get; }
        bool IsEnabled { get; set; }

        void FromIniValues(IEnumerable<string> values);
        IEnumerable<string> ToIniValues();
    }
}
