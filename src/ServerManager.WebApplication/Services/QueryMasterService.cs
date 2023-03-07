using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using QueryMaster;
using ServerManager.WebApplication.Models;
using ServerManager.WebApplication.Models.Data;

namespace ServerManager.WebApplication.Services;

public class QueryMasterService : IServerQueryService
{
    private readonly ServerQuerySettings _settings;

    public QueryMasterService(ServerQuerySettings settings)
    {
        _settings = settings;
    }

    public bool CheckServerStatus(string managerCode, string managerVersion, string ipString, int port)
    {
        ValidateServerStatusRequest(managerCode, ipString, port);

        try
        {
            using var server = ServerQuery.GetServerInstance(EngineType.Source, ipString, (ushort)port);
            return server.GetInfo() != null;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateServerStatusRequest(string managerCode, string ipString, int port)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(managerCode))
        {
            errors.Add("Manager code is required.");
        }
        else
        {
            var managerCodes = _settings.ManagerCodes ?? new List<ManagerCode>();
            if (!managerCodes.Any(c => c.Code.Equals(managerCode, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Manager code is invalid.");
            }
        }

        if (string.IsNullOrWhiteSpace(ipString))
        {
            errors.Add("IP Address is required.");
        }
        else if (!IPAddress.TryParse(ipString, out IPAddress _))
        {
            errors.Add("IP Address is invalid.");
        }

        if (port <= ushort.MinValue || port >= ushort.MaxValue)
        {
            errors.Add($"Valid port is required ({ushort.MinValue} to {ushort.MaxValue}).");
        }

        if (errors.Count > 0)
        {
            throw new ServerManagerApiException(StatusCodes.Status400BadRequest, errors);
        }
    }
}
