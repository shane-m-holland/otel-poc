using Microsoft.AspNetCore.Mvc;

namespace OtelPoc.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class LogController(ILogger<LogController> logger) : ControllerBase
{
    [HttpGet("/log")]
    public IActionResult Get([FromQuery] string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { error = "message query parameter is required" });

        logger.LogInformation("Forced log: {Message}", message);

        return Ok(new { message });
    }
}
