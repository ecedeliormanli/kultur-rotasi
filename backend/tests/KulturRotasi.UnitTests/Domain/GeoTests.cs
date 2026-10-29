using KulturRotasi.Domain.Common;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace KulturRotasi.UnitTests.Domain;

public class GeoTests
{
    [Fact]
    public void PrepareForGeography_turns_a_clockwise_polygon_counter_clockwise()
    {
        // Drawn clockwise (as many map tools do): SQL Server would read this as "the whole earth minus the box"
        var clockwise = Geo.Factory.CreatePolygon([
            new Coordinate(30, 37), new Coordinate(31, 37), new Coordinate(31, 36), new Coordinate(30, 36), new Coordinate(30, 37),
        ]);
        Assert.False(Orientation.IsCCW(clockwise.Shell.CoordinateSequence));

        var prepared = (Polygon)Geo.PrepareForGeography(clockwise);

        Assert.True(Orientation.IsCCW(prepared.Shell.CoordinateSequence));
        Assert.Equal(clockwise.Area, prepared.Area, precision: 9);
        Assert.Equal(Geo.Wgs84, prepared.SRID);
    }

    [Fact]
    public void PrepareForGeography_keeps_lines_as_they_are()
    {
        var line = Geo.Factory.CreateLineString([new Coordinate(29, 36.3), new Coordinate(30.6, 36.3)]);

        var prepared = Geo.PrepareForGeography(line);

        Assert.True(line.EqualsExact(prepared));
    }
}
