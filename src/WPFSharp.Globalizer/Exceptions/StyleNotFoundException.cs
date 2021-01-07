using System;
using System.Runtime.Serialization;
using System.Security;

namespace WPFSharp.Globalizer.Exceptions
{
    [Serializable]
    public class StyleNotFoundException : ArgumentException, ISerializable
    {
        public StyleNotFoundException() : base() { }

        public StyleNotFoundException(string message) : base(message) { }

        public StyleNotFoundException(string message, Exception innerException) : base(message, innerException) { }

        public StyleNotFoundException(string message, string invalidStyleName, Exception innerException) : base(message, innerException)
        {
            InvalidStyleName = invalidStyleName;
        }

        [SecuritySafeCritical]
        protected StyleNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public virtual string InvalidStyleName { get; private set; } = string.Empty;
    }
}
