using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using QueryMaster;
using ServerManager.WebApplication.Models;
using ServerManager.WebApplication.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ServerManager.WebApplication.Services
{
    public class QueryMasterService : IServerQueryService
    {
        internal const string CONFIG_MANAGERCODES = "ManagerCodes";

        private readonly IConfiguration _configuration;

        public QueryMasterService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool CheckServerStatus(string managerCode, string managerVersion, string ipString, int port)
        {
            ValidateServerStatusRequest(managerCode, ipString, port);

            try
            {
                using var server = ServerQuery.GetServerInstance(EngineType.Source, ipString, (ushort)port);

                var serverInfo = server.GetInfo();
                return serverInfo != null;
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
                var managerCodes = _configuration.GetSection(CONFIG_MANAGERCODES).Get<List<ManagerCode>>() ?? new List<ManagerCode>();
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
}
