using System;
using System.Collections.Generic;

namespace ServerManager.WebApplication.Models;

public class ServerManagerApiException : Exception
{
    public ServerManagerApiException()
    { }

    public ServerManagerApiException(int statusCode, ICollection<string> messages)
    {
        StatusCode = statusCode;
        Messages = messages;
    }

    public ServerManagerApiException(int statusCode, ICollection<string> messages, Exception innerException)
        : base(null, innerException)
    {
        StatusCode = statusCode;
        Messages = messages;
    }

    public int StatusCode { get; private set; }

    public ICollection<string> Messages { get; private set; } = new List<string>();
}