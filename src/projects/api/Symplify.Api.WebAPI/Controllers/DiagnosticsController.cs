using Microsoft.AspNetCore.Mvc;

namespace Symplify.Api.WebAPI.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpGet("alive")]
    public IActionResult Alive()
    {
        return Ok(new
        {
            status = "ok",
            service = "Symplify.Api.WebAPI",
            utcTime = DateTime.UtcNow
        });
    }
}
