using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OtelPoc.Api.Clients;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OtelPoc.Api.Tests;

public class SendEndpointTests
{
    [Fact]
    public async Task GetSend_CallsDownstreamReceiveEndpoint()
    {
        var capturedRequest = default(HttpRequestMessage);

        var fakeHandler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"serviceName":"service-b","correlationId":"test-id","received":true}""",
                    Encoding.UTF8, "application/json")
            };
        });

        var factory = new CustomWebApplicationFactory { DownstreamHandler = fakeHandler };
        var client = factory.CreateClient();

        var response = await client.GetAsync("/send");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/receive");
    }

    [Fact]
    public async Task GetSend_ForwardsCorrelationIdToDownstream()
    {
        var capturedCorrelationId = string.Empty;

        var fakeHandler = new FakeHttpMessageHandler(request =>
        {
            if (request.Headers.TryGetValues("X-Correlation-Id", out var values))
                capturedCorrelationId = values.First();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"serviceName":"service-b","correlationId":"captured","received":true}""",
                    Encoding.UTF8, "application/json")
            };
        });

        var factory = new CustomWebApplicationFactory { DownstreamHandler = fakeHandler };
        var client = factory.CreateClient();

        await client.GetAsync("/send");

        capturedCorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSend_ReturnsDownstreamResponse()
    {
        var fakeHandler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"serviceName":"service-b","correlationId":"abc","received":true}""",
                    Encoding.UTF8, "application/json")
            });

        var factory = new CustomWebApplicationFactory { DownstreamHandler = fakeHandler };
        var client = factory.CreateClient();

        var response = await client.GetAsync("/send");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("downstream").ValueKind.Should().Be(JsonValueKind.Object);
    }
}

internal class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(handler(request));
}
