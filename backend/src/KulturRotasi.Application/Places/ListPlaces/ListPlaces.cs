using KulturRotasi.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Places.ListPlaces;

/// <summary>Every place that matches the filters, without paging. Used to draw all markers on the map.</summary>
public sealed record ListPlacesQuery(PlaceFilter Filter, string Language);

public sealed class ListPlacesHandler(IAppDbContext db) : IQueryHandler<ListPlacesQuery, IReadOnlyList<PlaceSummaryDto>>
{
    public async Task<IReadOnlyList<PlaceSummaryDto>> HandleAsync(ListPlacesQuery query, CancellationToken cancellationToken) =>
        await db.Places.AsNoTracking()
            .ApplyFilter(query.Filter)
            .OrderBy(p => p.Id)
            .Select(PlaceQueries.ToSummary(query.Language))
            .ToListAsync(cancellationToken);
}
