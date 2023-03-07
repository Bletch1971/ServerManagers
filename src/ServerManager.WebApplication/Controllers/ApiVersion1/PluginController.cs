using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServerManager.WebApplication.Controllers.ApiVersion1;

[Route("api/plugin")]
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
public class PluginController : ControllerBase
{
    private readonly ILogger<PluginController> _logger;

    public PluginController(
        ILogger<PluginController> logger)
    {
        _logger = logger;
    }

    // GET: api/plugin/call/00000000-0000-0000-0000-000000000000/192.168.1.1
    [HttpGet()]
    [Route("call/{pluginCode}/{ipString}", Name = "PluginCall_V1")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<bool> PluginCall([FromRoute] string pluginCode, [FromRoute] string ipString)
    {
        try
        {
            _logger.LogInformation("Plugin call request made {pluginCode}; {ipString}", pluginCode, ipString);
            return Ok(true);
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError, false);
        }
    }
}
