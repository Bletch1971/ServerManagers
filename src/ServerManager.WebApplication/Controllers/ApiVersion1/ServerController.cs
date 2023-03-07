using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ServerManager.WebApplication.Models;
using ServerManager.WebApplication.Models.ApiVersion1;
using ServerManager.WebApplication.Services;

namespace ServerManager.WebApplication.Controllers.ApiVersion1;

[Route("api/server")]
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
public class ServerController : ControllerBase
{
    private readonly IServerQueryService _serverQueryService;
    private readonly ILogger<ServerController> _logger;

    public ServerController(
        IServerQueryService serverQueryService,
        ILogger<ServerController> logger)
    {
        _serverQueryService = serverQueryService;
        _logger = logger;
    }

    // GET: api/server/call/00000000-0000-0000-0000-000000000000/192.168.1.1
    [HttpGet()]
    [Route("call/{managerCode}/{ipString}", Name = "ServerCall")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<bool> ServerCall([FromRoute] string managerCode, [FromRoute] string ipString)
    {
        try
        {
            _logger.LogInformation("Server call request made {pluginCode}; {ipString}", managerCode, ipString);
            return Ok(true);
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError, false);
        }
    }

    // GET: api/server/00000000-0000-0000-0000-000000000000/1.0/192.168.1.1/27017
    [HttpGet()]
    [Route("{managerCode}/{managerVersion}/{ipString}/{port}", Name = "GetServerStatus")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<ServerStatusResponse> GetServerStatus([FromRoute] string managerCode, [FromRoute] string managerVersion, [FromRoute] string ipString, [FromRoute] int port)
    {
        // check for valid service
        if (_serverQueryService == null)
        {
            var response = new ErrorResponse 
            { 
                Errors = new List<string> 
                { 
                    "Server query service not available." 
                } 
            };
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }

        try
        {
            var stopWatch = Stopwatch.StartNew();

            var result = _serverQueryService.CheckServerStatus(managerCode, managerVersion, ipString, port);
            var response = new ServerStatusResponse 
            { 
                Available = result.ToString() 
            };

            stopWatch.Stop();
            _logger.LogInformation("Server status request made {managerCode}; {managerVersion}; {ipString}; {port}; {timeTaken}", managerCode, managerVersion, ipString, port, stopWatch.Elapsed);
            return Ok(response);
        }
        catch (ServerManagerApiException ex)
        {
            var response = new ErrorResponse 
            { 
                Errors = ex.Messages 
            };
            return StatusCode(ex.StatusCode, response);
        }
        catch (Exception ex)
        {
            var response = new ErrorResponse 
            { 
                Errors = new List<string> 
                { 
                    ex.Message 
                } 
            };
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }
}
