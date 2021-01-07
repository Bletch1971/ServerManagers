using System;
using System.Runtime.Serialization;
using System.Security;

namespace ServerManagerTool.Plugin.Common
{
    public class PluginException : Exception
    {
        public PluginException()
            : base()
        {
        }

        public PluginException(string message)
            : base(message)
        {
        }

        public PluginException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [SecuritySafeCritical]
        protected PluginException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
