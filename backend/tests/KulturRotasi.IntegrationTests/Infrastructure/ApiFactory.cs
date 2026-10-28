using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace KulturRotasi.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a real SQL Server in Docker (Testcontainers), points the API at it,
/// and lets the API migrate and seed it on startup. One container is shared by all tests.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Database:MigrateAndSeedOnStartup", "true");
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
