using System;

namespace ServerManagerTool.Plugin.Common.Events
{
    public class ResourceDictionaryChangedEventArgs : EventArgs
    {
        public ResourceDictionaryChangedEventArgs(string languageCode)
        {
            LanguageCode = languageCode;
        }

        public string LanguageCode;
    }
}
