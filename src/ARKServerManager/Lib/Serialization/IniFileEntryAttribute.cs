using ServerManagerTool.Common.Attibutes;
using ServerManagerTool.Enums;

namespace ServerManagerTool.Lib
{
    public class IniFileEntryAttribute : BaseIniFileEntryAttribute
    {
        public IniFileEntryAttribute(IniFiles file, IniSections section, ServerProfileCategory category, string key = "")
            : base(file, section, category, key)
        {
        }
    }
}
