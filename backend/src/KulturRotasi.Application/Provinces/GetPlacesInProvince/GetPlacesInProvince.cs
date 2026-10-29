using KulturRotasi.Application.Abstractions;
using KulturRotasi.Application.Places;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Provinces.GetPlacesInProvince;

/// <summary>Places whose location falls inside the province border. Returns null for an unknown plate code.</summary>
public sealed record GetPlacesInProvinceQuery(int PlateCode, string Language);

public sealed class GetPlacesInProvinceHandler(IAppDbContext db)
    : IQueryHandler<GetPlacesInProvinceQuery, IReadOnlyList<PlaceSummaryDto>?>
{
    public async Task<IReadOnlyList<PlaceSummaryDto>?> HandleAsync(GetPlacesInProvinceQuery query, CancellationToken cancellationToken)
    {
        if (!await db.Provinces.AnyAsync(p => p.Id == query.PlateCode, cancellationToken))
        {
            return null;
        }

        // A spatial join done entirely in SQL Server: the border never leaves the database
        return await db.Places.AsNoTracking()
            .Where(place => db.Provinces.Any(p => p.Id == query.PlateCode && p.Boundary.Contains(place.Location)))
            .OrderBy(PlaceQueries.NameIn(query.Language))
            .Select(PlaceQueries.ToSummary(query.Language))
            .ToListAsync(cancellationToken);
    }
}
