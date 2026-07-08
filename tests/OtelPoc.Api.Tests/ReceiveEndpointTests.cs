using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OtelPoc.Api.Tests;

public class ReceiveEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostReceive_Returns200()
    {
        var response = await _client.PostAsync("/receive", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostReceive_ReturnsServiceName()
    {
        var response = await _client.PostAsync("/receive", null);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("serviceName", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PostReceive_EchosCorrelationIdHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/receive");
        request.Headers.Add("X-Correlation-Id", "test-correlation-123");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("correlationId").GetString().Should().Be("test-correlation-123");
    }

    [Fact]
    public async Task GetReceive_Returns200()
    {
        var response = await _client.GetAsync("/receive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
