using FluentValidation;
using KulturRotasi.Application.Abstractions;
using KulturRotasi.Domain.Common;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Application.Places.GetPlacesWithinGeometry;

/// <summary>
/// Places inside a shape drawn on the map (point-in-polygon), or within <see cref="BufferKm"/> of it.
/// A line with a buffer answers questions like "what can I visit within 10 km of this road?".
/// </summary>
public sealed record GetPlacesWithinGeometryQuery(Geometry Geometry, string Language, double BufferKm = 0);

public sealed class GetPlacesWithinGeometryQueryValidator : AbstractValidator<GetPlacesWithinGeometryQuery>
{
    public const double MaxBufferKm = 100;
    public const int MaxVertices = 2_000;

    public GetPlacesWithinGeometryQueryValidator()
    {
        RuleFor(q => q.BufferKm).InclusiveBetween(0, MaxBufferKm);

        RuleFor(q => q.Geometry)
            .NotNull()
            .Must(g => g is Point or LineString or Polygon or MultiPoint or MultiLineString or MultiPolygon)
            .WithMessage("Geometry must be a Point, LineString, Polygon or their Multi variants.")
            .Must(g => g.NumPoints <= MaxVertices)
            .WithMessage($"Geometry may have at most {MaxVertices} vertices.")
            .Must(g => g.Coordinates.All(c => c.Y is >= -90 and <= 90 && c.X is >= -180 and <= 180))
            .WithMessage("Coordinates must be [longitude, latitude] in WGS 84.")
            .Must(g => g.EnvelopeInternal.Width < 180 && g.EnvelopeInternal.Height < 90)
            .WithMessage("Geometry must be smaller than a hemisphere.")
            .Must(g => g.IsValid)
            .WithMessage("Geometry is not valid (for example, a polygon edge crosses itself).");

        RuleFor(q => q.BufferKm)
            .GreaterThan(0)
            .When(q => q.Geometry is Point or MultiPoint)
            .WithMessage("A point needs a buffer greater than 0 km.");
    }
}

public sealed class GetPlacesWithinGeometryHandler(IAppDbContext db)
    : IQueryHandler<GetPlacesWithinGeometryQuery, IReadOnlyList<PlaceSummaryDto>>
{
    public async Task<IReadOnlyList<PlaceSummaryDto>> HandleAsync(GetPlacesWithinGeometryQuery query, CancellationToken cancellationToken)
    {
        // A polygon drawn clockwise would otherwise mean "everywhere except this area" to SQL Server
        var area = Geo.PrepareForGeography(query.Geometry);
        var bufferMetres = query.BufferKm * 1000;

        // Distance 0 means "inside or touching"; a positive distance acts as a buffer around the shape
        return await db.Places.AsNoTracking()
            .Where(p => p.Location.IsWithinDistance(area, bufferMetres))
            .OrderBy(PlaceQueries.NameIn(query.Language))
            .Select(PlaceQueries.ToSummary(query.Language))
            .ToListAsync(cancellationToken);
    }
}
