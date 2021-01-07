using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace QueryMaster
{
    /// <summary>
    /// The exception that is thrown by the QueryMaster library
    /// </summary>
    [Serializable]
    public class QueryMasterException : Exception
    {
        public QueryMasterException() : base() { }
        public QueryMasterException(string message) : base(message) { }
        public QueryMasterException(string message, Exception innerException) : base(message, innerException) { }
        protected QueryMasterException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
    /// <summary>
    /// The exception that is thrown when an invalid message header is received
    /// </summary>
    [Serializable]
    public class InvalidHeaderException : QueryMasterException
    {
        public InvalidHeaderException() : base() { }
        public InvalidHeaderException(string message) : base(message) { }
        public InvalidHeaderException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidHeaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// The exception that is thrown when an invalid packet is received
    /// </summary>
    [Serializable]
    public class InvalidPacketException : QueryMasterException
    {
        public InvalidPacketException() : base() { }
        public InvalidPacketException(string message) : base(message) { }
        public InvalidPacketException(string message, Exception innerException) : base(message, innerException) { }
        protected InvalidPacketException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    /// <summary>
    /// The exception that is thrown when there is an error while parsing received packets
    /// </summary>
    [Serializable]
    public class ParseException : QueryMasterException
    {
        public ParseException() : base() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
        protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
