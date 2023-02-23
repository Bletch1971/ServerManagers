using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ServerManager.WebApplication.Controllers;

[Route("api/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    [HttpGet()]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get()
    {
        try
        {
            return Ok();
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError, false);
        }
    }
}
