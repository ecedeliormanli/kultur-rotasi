using FluentValidation;
using KulturRotasi.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Simplify;

namespace KulturRotasi.Application.Provinces.GetProvinceBoundaries;

/// <summary>
/// Province borders for the map. <see cref="ToleranceDegrees"/> simplifies the outlines so the response
/// stays small; 0.005° (about 500 m) is invisible at country zoom level.
/// </summary>
public sealed record GetProvinceBoundariesQuery(double ToleranceDegrees = 0.005);

public sealed class GetProvinceBoundariesQueryValidator : AbstractValidator<GetProvinceBoundariesQuery>
{
    public GetProvinceBoundariesQueryValidator()
    {
        RuleFor(q => q.ToleranceDegrees).InclusiveBetween(0, 0.1);
    }
}

public sealed class GetProvinceBoundariesHandler(IAppDbContext db)
    : IQueryHandler<GetProvinceBoundariesQuery, IReadOnlyList<ProvinceBoundaryDto>>
{
    public async Task<IReadOnlyList<ProvinceBoundaryDto>> HandleAsync(GetProvinceBoundariesQuery query, CancellationToken cancellationToken)
    {
        var provinces = await db.Provinces.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProvinceBoundaryDto(
                p.Id,
                p.Name,
                db.Places.Count(place => p.Boundary.Contains(place.Location)),
                p.Boundary))
            .ToListAsync(cancellationToken);

        if (query.ToleranceDegrees == 0)
        {
            return provinces;
        }

        // Topology-preserving: neighbouring borders may move slightly but polygons never collapse or cross
        return provinces
            .Select(p => p with { Boundary = TopologyPreservingSimplifier.Simplify(p.Boundary, query.ToleranceDegrees) })
            .ToList();
    }
}
