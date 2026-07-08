using Microsoft.AspNetCore.Mvc;

namespace OtelPoc.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ReceiveController(ILogger<ReceiveController> logger) : ControllerBase
{
    [HttpGet("/receive")]
    [HttpPost("/receive")]
    public IActionResult Receive()
    {
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? string.Empty;
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "otel-poc-api";

        logger.LogInformation("Received request. CorrelationId={CorrelationId} Service={ServiceName}",
            correlationId, serviceName);

        return Ok(new
        {
            serviceName,
            correlationId,
            received = true,
            instanceId = Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID") ?? "default"
        });
    }
}
