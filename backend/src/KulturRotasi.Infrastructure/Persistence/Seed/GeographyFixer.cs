using KulturRotasi.Domain.Common;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace KulturRotasi.Infrastructure.Persistence.Seed;

/// <summary>
/// Prepares polygons for SQL Server's <c>geography</c> type, which is stricter than plain GeoJSON:
/// the geometry must be valid, and an outer ring must run counter-clockwise (holes clockwise).
/// With the wrong orientation SQL Server reads the polygon as "the whole earth minus this area".
/// </summary>
internal static class GeographyFixer
{
    public static Geometry Fix(Geometry geometry)
    {
        var valid = GeometryFixer.Fix(geometry);

        Geometry fixedGeometry = valid switch
        {
            Polygon polygon => Orient(polygon),
            MultiPolygon multi => Geo.Factory.CreateMultiPolygon(
                multi.Geometries.Cast<Polygon>().Select(Orient).ToArray()),
            _ => throw new InvalidOperationException($"Expected a polygon, got {valid.GeometryType}."),
        };

        fixedGeometry.SRID = Geo.Wgs84;
        return fixedGeometry;
    }

    private static Polygon Orient(Polygon polygon)
    {
        var shell = EnsureOrientation(polygon.Shell, counterClockwise: true);
        var holes = polygon.Holes.Select(h => EnsureOrientation(h, counterClockwise: false)).ToArray();
        return Geo.Factory.CreatePolygon(shell, holes);
    }

    private static LinearRing EnsureOrientation(LinearRing ring, bool counterClockwise) =>
        Orientation.IsCCW(ring.CoordinateSequence) == counterClockwise
            ? ring
            : (LinearRing)ring.Reverse();
}
