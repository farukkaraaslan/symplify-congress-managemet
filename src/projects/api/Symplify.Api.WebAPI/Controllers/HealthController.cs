using Microsoft.AspNetCore.Mvc;

namespace Symplify.Api.WebAPI.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "Symplify.Api.WebAPI",
            utcTime = DateTime.UtcNow
        });
    }
}
