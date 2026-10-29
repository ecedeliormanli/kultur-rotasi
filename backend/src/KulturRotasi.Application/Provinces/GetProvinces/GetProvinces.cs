using KulturRotasi.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Provinces.GetProvinces;

/// <summary>All 81 provinces with the number of places inside each one.</summary>
public sealed record GetProvincesQuery;

public sealed class GetProvincesHandler(IAppDbContext db) : IQueryHandler<GetProvincesQuery, IReadOnlyList<ProvinceDto>>
{
    public async Task<IReadOnlyList<ProvinceDto>> HandleAsync(GetProvincesQuery query, CancellationToken cancellationToken) =>
        await db.Provinces.AsNoTracking()
            .OrderBy(p => p.Id)
            // Counted spatially (point-in-polygon), not through the ProvinceId column
            .Select(p => new ProvinceDto(p.Id, p.Name, db.Places.Count(place => p.Boundary.Contains(place.Location))))
            .ToListAsync(cancellationToken);
}
