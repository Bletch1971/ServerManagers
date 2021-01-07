namespace ServerManagerTool.Common.Interfaces
{
    public interface IIniSectionCollection
    {
        IIniValuesCollection[] Sections { get; }

        void Add(string sectionName, string[] values);

        void Update();
    }
}
