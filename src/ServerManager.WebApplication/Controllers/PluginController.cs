using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServerManager.WebApplication.Controllers
{
    [Route("api/plugin")]
    [ApiController]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class PluginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServerController> _logger;

        public PluginController(IConfiguration configuration, ILogger<ServerController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // GET: api/plugin/call/00000000-0000-0000-0000-000000000000/192.168.1.1
        [HttpGet()]
        [Route("call/{pluginCode}/{ipString}", Name = "PluginCall_V1")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<bool> PluginCall_V1([FromRoute] string pluginCode, [FromRoute] string ipString)
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
    }
}
