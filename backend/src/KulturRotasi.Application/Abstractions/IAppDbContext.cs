using KulturRotasi.Domain.Places;
using KulturRotasi.Domain.Provinces;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Abstractions;

/// <summary>
/// The database as seen by the use cases. Implemented by <c>AppDbContext</c> in Infrastructure,
/// so handlers query with LINQ without knowing about SQL Server or connection strings.
/// </summary>
public interface IAppDbContext
{
    DbSet<Place> Places { get; }
    DbSet<Province> Provinces { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
