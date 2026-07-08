using Microsoft.AspNetCore.Mvc;
using OtelPoc.Api.Clients;
using System.Text.Json;

namespace OtelPoc.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SendController(DownstreamClient downstream, ILogger<SendController> logger) : ControllerBase
{
    [HttpGet("/send")]
    [HttpPost("/send")]
    public async Task<IActionResult> Send(CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString();
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "otel-poc-api";

        logger.LogInformation("Sending request to downstream. CorrelationId={CorrelationId} From={ServiceName}",
            correlationId, serviceName);

        var downstreamJson = await downstream.SendAsync(correlationId, ct);
        var downstreamDoc = JsonDocument.Parse(downstreamJson);

        return Ok(new
        {
            serviceName,
            correlationId,
            downstream = downstreamDoc.RootElement
        });
    }
}
