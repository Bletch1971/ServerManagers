using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerManager.WebApplication.Models;
using ServerManager.WebApplication.Models.ApiVersion1;
using ServerManager.WebApplication.Services;
using System;
using System.Collections.Generic;

namespace ServerManager.WebApplication.Controllers
{
    [Route("api/server")]
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class ServerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServerController> _logger;
        private readonly IServerQueryService _serverQueryService;

        public ServerController(IConfiguration configuration, ILogger<ServerController> logger, IServerQueryService serverQueryService)
        {
            _configuration = configuration;
            _logger = logger;
            _serverQueryService = serverQueryService;
        }

        // GET: api/server/call/00000000-0000-0000-0000-000000000000/192.168.1.1
        [HttpGet()]
        [Route("call/{managerCode}/{ipString}", Name = "Call_V1")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<bool> Call_V1([FromRoute] string managerCode, [FromRoute] string ipString)
        {
            try
            {
                return Ok(true);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, false);
            }
        }

        // GET: api/server/192.168.1.1/27017
        [HttpGet()]
        [Route("{ipString}/{port}", Name = "GetServerStatus_V1")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public ActionResult<ServerStatusResponse> GetServerStatus_V1([FromRoute] string ipString, [FromRoute] int port)
        {
            // check for valid service
            if (_serverQueryService == null)
            {
                var response = new ErrorResponse { Errors = new List<string> { "Server query service not available." } };
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }

            try
            {
                var result = _serverQueryService.CheckServerStatus(Guid.Empty.ToString(), "0.0", ipString, port);
                var response = new ServerStatusResponse { Available = result.ToString() };
                return Ok(response);
            }
            catch (ServerManagerApiException ex)
            {
                var response = new ErrorResponse { Errors = ex.Messages };
                return StatusCode(ex.StatusCode, response);
            }
            catch (Exception ex)
            {
                var response = new ErrorResponse { Errors = new List<string> { ex.Message } };
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        // GET: api/server/00000000-0000-0000-0000-000000000000/192.168.1.1/27017
        [HttpGet()]
        [Route("{managerCode}/{ipString}/{port}", Name = "GetServerStatus_V2")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public ActionResult<ServerStatusResponse> GetServerStatus_V2([FromRoute] string managerCode, [FromRoute] string ipString, [FromRoute] int port)
        {
            // check for valid service
            if (_serverQueryService == null)
            {
                var response = new ErrorResponse { Errors = new List<string> { "Server query service not available." } };
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }

            try
            {
                var result = _serverQueryService.CheckServerStatus(managerCode, "0.0", ipString, port);
                var response = new ServerStatusResponse { Available = result.ToString() };
                return Ok(response);
            }
            catch (ServerManagerApiException ex)
            {
                var response = new ErrorResponse { Errors = ex.Messages };
                return StatusCode(ex.StatusCode, response);
            }
            catch (Exception ex)
            {
                var response = new ErrorResponse { Errors = new List<string> { ex.Message } };
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        // GET: api/server/00000000-0000-0000-0000-000000000000/1.0/192.168.1.1/27017
        [HttpGet()]
        [Route("{managerCode}/{managerVersion}/{ipString}/{port}", Name = "GetServerStatus_V3")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public ActionResult<ServerStatusResponse> GetServerStatus_V3([FromRoute] string managerCode, [FromRoute] string managerVersion, [FromRoute] string ipString, [FromRoute] int port)
        {
            // check for valid service
            if (_serverQueryService == null)
            {
                var response = new ErrorResponse { Errors = new List<string> { "Server query service not available." } };
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }

            try
            {
                var result = _serverQueryService.CheckServerStatus(managerCode, managerVersion, ipString, port);
                var response = new ServerStatusResponse { Available = result.ToString() };
                return Ok(response);
            }
            catch (ServerManagerApiException ex)
            {
                var response = new ErrorResponse { Errors = ex.Messages };
                return StatusCode(ex.StatusCode, response);
            }
            catch (Exception ex)
            {
                var response = new ErrorResponse { Errors = new List<string> { ex.Message } };
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }
    }
}
