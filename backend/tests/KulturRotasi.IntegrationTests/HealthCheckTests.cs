using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KulturRotasi.IntegrationTests;

/// <summary>Boots the whole API in memory and calls it over HTTP.</summary>
public class HealthCheckTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_endpoint_returns_healthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
