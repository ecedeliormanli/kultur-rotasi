using FluentValidation;
using KulturRotasi.Application.Abstractions;
using KulturRotasi.Domain.Common;
using KulturRotasi.Domain.Places;
using Microsoft.EntityFrameworkCore;

namespace KulturRotasi.Application.Places.GetNearbyPlaces;

/// <summary>
/// The places closest to a point, nearest first. With <see cref="RadiusKm"/> only places within that distance
/// are returned (a circular buffer around the point); without it, simply the nearest <see cref="Limit"/> places.
/// </summary>
public sealed record GetNearbyPlacesQuery(
    double Latitude,
    double Longitude,
    string Language,
    double? RadiusKm = null,
    PlaceType? Type = null,
    int Limit = 10);

public sealed class GetNearbyPlacesQueryValidator : AbstractValidator<GetNearbyPlacesQuery>
{
    public const double MaxRadiusKm = 500;
    public const int MaxLimit = 100;

    public GetNearbyPlacesQueryValidator()
    {
        RuleFor(q => q.Latitude).InclusiveBetween(-90, 90);
        RuleFor(q => q.Longitude).InclusiveBetween(-180, 180);
        RuleFor(q => q.RadiusKm).GreaterThan(0).LessThanOrEqualTo(MaxRadiusKm);
        RuleFor(q => q.Limit).InclusiveBetween(1, MaxLimit);
    }
}

public sealed class GetNearbyPlacesHandler(IAppDbContext db) : IQueryHandler<GetNearbyPlacesQuery, IReadOnlyList<NearbyPlaceDto>>
{
    public async Task<IReadOnlyList<NearbyPlaceDto>> HandleAsync(GetNearbyPlacesQuery query, CancellationToken cancellationToken)
    {
        var origin = Geo.CreatePoint(query.Latitude, query.Longitude);
        var places = db.Places.AsNoTracking().ApplyFilter(new PlaceFilter(query.Type));

        if (query.RadiusKm is { } radiusKm)
        {
            // Becomes STDistance(...) <= @radius in SQL, which can use the spatial index
            places = places.Where(p => p.Location.IsWithinDistance(origin, radiusKm * 1000));
        }

        return await places
            .OrderBy(p => p.Location.Distance(origin))
            .Take(query.Limit)
            .Select(PlaceQueries.ToNearby(query.Language, origin))
            .ToListAsync(cancellationToken);
    }
}
