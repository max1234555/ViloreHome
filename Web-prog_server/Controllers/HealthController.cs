using Microsoft.AspNetCore.Mvc;

namespace Web_prog_server.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("ok");
}
