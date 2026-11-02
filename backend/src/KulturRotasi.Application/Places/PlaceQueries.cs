using System.Linq.Expressions;
using KulturRotasi.Domain.Common;
using KulturRotasi.Domain.Places;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Application.Places;

/// <summary>
/// Reusable pieces of LINQ for place queries. They are expression trees, so EF Core turns them into SQL
/// (the language fallback becomes a subquery, X/Y become geography .Long/.Lat) instead of running them in memory.
/// </summary>
internal static class PlaceQueries
{
    public static IQueryable<Place> ApplyFilter(this IQueryable<Place> places, PlaceFilter filter)
    {
        if (filter.Type is { } type)
        {
            places = places.Where(p => p.Type == type);
        }

        if (filter.ProvinceId is { } provinceId)
        {
            places = places.Where(p => p.ProvinceId == provinceId);
        }

        if (filter.IsUnescoWorldHeritage is { } unesco)
        {
            places = places.Where(p => p.IsUnescoWorldHeritage == unesco);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // Matches the name in any language, so "efes" and "ephesus" both find Ephesus
            var search = filter.Search.Trim();
            places = places.Where(p => p.Translations.Any(t => t.Name.Contains(search)));
        }

        return places;
    }

    public static Expression<Func<Place, string>> NameIn(string language) => p =>
        p.Translations.Where(t => t.LanguageCode == language).Select(t => t.Name).FirstOrDefault()
        ?? p.Translations.Where(t => t.LanguageCode == LanguageCode.Turkish).Select(t => t.Name).FirstOrDefault()
        ?? p.Slug;

    public static Expression<Func<Place, PlaceSummaryDto>> ToSummary(string language) => p =>
        new PlaceSummaryDto(
            p.Id,
            p.Slug,
            p.Translations.Where(t => t.LanguageCode == language).Select(t => t.Name).FirstOrDefault()
                ?? p.Translations.Where(t => t.LanguageCode == LanguageCode.Turkish).Select(t => t.Name).FirstOrDefault()
                ?? p.Slug,
            p.Type,
            p.Location.Y,
            p.Location.X,
            p.ProvinceId,
            p.Province.Name,
            p.IsUnescoWorldHeritage);

    /// <summary>On a geography column, Distance is in metres and follows the curve of the earth.</summary>
    public static Expression<Func<Place, NearbyPlaceDto>> ToNearby(string language, Point origin) => p =>
        new NearbyPlaceDto(
            p.Id,
            p.Slug,
            p.Translations.Where(t => t.LanguageCode == language).Select(t => t.Name).FirstOrDefault()
                ?? p.Translations.Where(t => t.LanguageCode == LanguageCode.Turkish).Select(t => t.Name).FirstOrDefault()
                ?? p.Slug,
            p.Type,
            p.Location.Y,
            p.Location.X,
            p.ProvinceId,
            p.Province.Name,
            p.IsUnescoWorldHeritage,
            Math.Round(p.Location.Distance(origin) / 1000, 2));
}
