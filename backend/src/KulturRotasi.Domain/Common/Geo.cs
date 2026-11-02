using NetTopologySuite;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

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

    /// <summary>
    /// Prepares a geometry for SQL Server's <c>geography</c> type, which is stricter than plain GeoJSON:
    /// the geometry must be valid, and a polygon's outer ring must run counter-clockwise (holes clockwise).
    /// With the wrong orientation SQL Server reads the polygon as "the whole earth minus this area".
    /// </summary>
    public static Geometry PrepareForGeography(Geometry geometry)
    {
        var valid = GeometryFixer.Fix(geometry);

        var prepared = valid switch
        {
            Polygon polygon => Orient(polygon),
            MultiPolygon multi => Factory.CreateMultiPolygon(multi.Geometries.Cast<Polygon>().Select(Orient).ToArray()),
            _ => valid,
        };

        prepared.SRID = Wgs84;
        return prepared;
    }

    private static Polygon Orient(Polygon polygon)
    {
        var shell = EnsureOrientation(polygon.Shell, counterClockwise: true);
        var holes = polygon.Holes.Select(h => EnsureOrientation(h, counterClockwise: false)).ToArray();
        return Factory.CreatePolygon(shell, holes);
    }

    private static LinearRing EnsureOrientation(LinearRing ring, bool counterClockwise) =>
        Orientation.IsCCW(ring.CoordinateSequence) == counterClockwise
            ? ring
            : (LinearRing)ring.Reverse();
}
