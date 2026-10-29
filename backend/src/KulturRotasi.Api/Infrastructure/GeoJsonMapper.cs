using KulturRotasi.Application.Places;
using KulturRotasi.Application.Provinces;
using KulturRotasi.Domain.Common;
using NetTopologySuite.Features;

namespace KulturRotasi.Api.Infrastructure;

/// <summary>
/// Builds GeoJSON (RFC 7946) feature collections, the format Leaflet, OpenLayers, QGIS and GeoServer all read.
/// Each feature is a geometry plus a flat set of properties.
/// </summary>
internal static class GeoJsonMapper
{
    public const string ContentType = "application/geo+json";

    public static FeatureCollection ToFeatureCollection(IEnumerable<PlaceSummaryDto> places)
    {
        var collection = new FeatureCollection();
        foreach (var place in places)
        {
            collection.Add(new Feature(
                Geo.CreatePoint(place.Latitude, place.Longitude),
                new AttributesTable
                {
                    { "id", place.Id },
                    { "slug", place.Slug },
                    { "name", place.Name },
                    { "type", place.Type.ToString() },
                    { "provinceId", place.ProvinceId },
                    { "provinceName", place.ProvinceName },
                    { "isUnescoWorldHeritage", place.IsUnescoWorldHeritage },
                }));
        }

        return collection;
    }

    public static FeatureCollection ToFeatureCollection(IEnumerable<ProvinceBoundaryDto> provinces)
    {
        var collection = new FeatureCollection();
        foreach (var province in provinces)
        {
            collection.Add(new Feature(
                province.Boundary,
                new AttributesTable
                {
                    { "plateCode", province.PlateCode },
                    { "name", province.Name },
                    { "placeCount", province.PlaceCount },
                }));
        }

        return collection;
    }
}
