using KulturRotasi.Application.Places;
using KulturRotasi.Application.Places.GetNearbyPlaces;
using KulturRotasi.Application.Places.GetPlacesWithinGeometry;
using KulturRotasi.Application.Places.SearchPlaces;
using KulturRotasi.Domain.Common;
using NetTopologySuite.Geometries;

namespace KulturRotasi.UnitTests.Application;

public class ValidatorTests
{
    [Theory]
    [InlineData(39.92, 32.85, 50.0, 10, true)]
    [InlineData(39.92, 32.85, null, 10, true)]
    [InlineData(91, 32.85, null, 10, false)]
    [InlineData(39.92, 181, null, 10, false)]
    [InlineData(39.92, 32.85, 0.0, 10, false)]
    [InlineData(39.92, 32.85, 501.0, 10, false)]
    [InlineData(39.92, 32.85, null, 0, false)]
    [InlineData(39.92, 32.85, null, 101, false)]
    public void Nearby_query_limits(double latitude, double longitude, double? radiusKm, int limit, bool expectedValid)
    {
        var query = new GetNearbyPlacesQuery(latitude, longitude, LanguageCode.Turkish, radiusKm, Limit: limit);

        var result = new GetNearbyPlacesQueryValidator().Validate(query);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void Search_query_rejects_large_pages()
    {
        var query = new SearchPlacesQuery(new PlaceFilter(), LanguageCode.Turkish, Page: 1, PageSize: 1000);

        var result = new SearchPlacesQueryValidator().Validate(query);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchPlacesQuery.PageSize));
    }

    [Fact]
    public void Within_query_needs_a_buffer_around_a_point()
    {
        var point = Geo.CreatePoint(37.94, 27.34);
        var validator = new GetPlacesWithinGeometryQueryValidator();

        Assert.False(validator.Validate(new GetPlacesWithinGeometryQuery(point, LanguageCode.Turkish)).IsValid);
        Assert.True(validator.Validate(new GetPlacesWithinGeometryQuery(point, LanguageCode.Turkish, BufferKm: 10)).IsValid);
    }

    [Fact]
    public void Within_query_rejects_a_self_intersecting_polygon()
    {
        // A "bow tie": the edges cross in the middle
        var bowTie = Geo.Factory.CreatePolygon([
            new Coordinate(30, 36), new Coordinate(31, 37), new Coordinate(31, 36), new Coordinate(30, 37), new Coordinate(30, 36),
        ]);

        var result = new GetPlacesWithinGeometryQueryValidator().Validate(new GetPlacesWithinGeometryQuery(bowTie, LanguageCode.Turkish));

        Assert.False(result.IsValid);
    }
}
