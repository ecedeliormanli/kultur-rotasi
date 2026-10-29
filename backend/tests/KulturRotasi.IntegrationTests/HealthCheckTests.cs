using System.Net;
using KulturRotasi.IntegrationTests.Infrastructure;

namespace KulturRotasi.IntegrationTests;

/// <summary>Boots the whole API in memory and calls it over HTTP.</summary>
[Collection(ApiTestGroup.Name)]
public class HealthCheckTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_endpoint_returns_healthy_when_database_is_reachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
