using KulturRotasi.Application.Abstractions;
using KulturRotasi.Domain.Places;
using KulturRotasi.Domain.Provinces;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Province> Provinces => Set<Province>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in this assembly (Configurations/ folder)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
