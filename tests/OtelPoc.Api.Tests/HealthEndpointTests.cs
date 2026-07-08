using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace OtelPoc.Api.Tests;

public class HealthEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("status").GetString().Should().Be("healthy");
    }

    [Fact]
    public async Task GetHealth_ReturnsServiceName()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        doc.RootElement.TryGetProperty("serviceName", out _).Should().BeTrue();
    }
}
