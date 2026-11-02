using KulturRotasi.Domain.Places;

namespace KulturRotasi.Application.Places;

/// <summary>What a list or a map marker needs to show a place. The name is in the requested language.</summary>
public sealed record PlaceSummaryDto(
    int Id,
    string Slug,
    string Name,
    PlaceType Type,
    double Latitude,
    double Longitude,
    int ProvinceId,
    string ProvinceName,
    bool IsUnescoWorldHeritage);

/// <summary>A place with its distance from the point the user searched around.</summary>
public sealed record NearbyPlaceDto(
    int Id,
    string Slug,
    string Name,
    PlaceType Type,
    double Latitude,
    double Longitude,
    int ProvinceId,
    string ProvinceName,
    bool IsUnescoWorldHeritage,
    double DistanceKm);

/// <summary>Full details of a place. <see cref="Language"/> says which language the text is in (it falls back to Turkish).</summary>
public sealed record PlaceDetailsDto(
    int Id,
    string Slug,
    string Language,
    string Name,
    string Description,
    PlaceType Type,
    double Latitude,
    double Longitude,
    int ProvinceId,
    string ProvinceName,
    bool IsUnescoWorldHeritage);

/// <summary>Optional filters shared by the list and map endpoints.</summary>
public sealed record PlaceFilter(
    PlaceType? Type = null,
    int? ProvinceId = null,
    bool? IsUnescoWorldHeritage = null,
    string? Search = null);
