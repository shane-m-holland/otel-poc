using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace OtelPoc.Api.Tests;

public class LogEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetLog_WithMessage_Returns200AndEchosMessage()
    {
        var response = await _client.GetAsync("/log?message=hello-world");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("message").GetString().Should().Be("hello-world");
    }

    [Fact]
    public async Task GetLog_WithoutMessage_Returns400()
    {
        var response = await _client.GetAsync("/log");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLog_WithEmptyMessage_Returns400()
    {
        var response = await _client.GetAsync("/log?message=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
