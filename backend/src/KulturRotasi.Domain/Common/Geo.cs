using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Domain.Common;

/// <summary>Creates geometries in WGS 84 (SRID 4326), the latitude/longitude system used by GPS and web maps.</summary>
public static class Geo
{
    public const int Wgs84 = 4326;

    public static readonly GeometryFactory Factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: Wgs84);

    /// <summary>Note the order: a point's X is the longitude and its Y is the latitude.</summary>
    public static Point CreatePoint(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new DomainException($"Latitude must be between -90 and 90, got {latitude}.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new DomainException($"Longitude must be between -180 and 180, got {longitude}.");
        }

        return Factory.CreatePoint(new Coordinate(longitude, latitude));
    }
}
