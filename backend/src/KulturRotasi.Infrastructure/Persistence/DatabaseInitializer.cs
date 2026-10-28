using KulturRotasi.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KulturRotasi.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds sample data. Meant for local development and tests;
    /// a production deployment would run migrations as a separate, reviewed step.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
