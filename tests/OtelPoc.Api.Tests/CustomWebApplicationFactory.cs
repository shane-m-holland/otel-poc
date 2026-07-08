using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OtelPoc.Api.Clients;

namespace OtelPoc.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public HttpMessageHandler? DownstreamHandler { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            if (DownstreamHandler is not null)
            {
                services.AddHttpClient<DownstreamClient>(client =>
                {
                    client.BaseAddress = new Uri("http://fake-downstream");
                }).ConfigurePrimaryHttpMessageHandler(() => DownstreamHandler);
            }
        });
    }
}
