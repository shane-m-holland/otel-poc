using Microsoft.AspNetCore.Mvc;

namespace OtelPoc.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "otel-poc-api",
            instanceId = Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID") ?? "default"
        });
    }
}
