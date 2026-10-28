using KulturRotasi.Domain.Common;
using KulturRotasi.Domain.Places;

namespace KulturRotasi.UnitTests.Domain;

public class PlaceTests
{
    [Fact]
    public void Create_stores_location_with_longitude_as_x_and_latitude_as_y()
    {
        var place = Place.Create("Ephesus", PlaceType.ArchaeologicalSite, latitude: 37.9395, longitude: 27.3417, provinceId: 35);

        Assert.Equal("ephesus", place.Slug);
        Assert.Equal(27.3417, place.Location.X);
        Assert.Equal(37.9395, place.Location.Y);
        Assert.Equal(Geo.Wgs84, place.Location.SRID);
    }

    [Theory]
    [InlineData(91, 30)]
    [InlineData(-91, 30)]
    [InlineData(39, 181)]
    [InlineData(39, -181)]
    public void Create_rejects_coordinates_outside_the_globe(double latitude, double longitude)
    {
        Assert.Throws<DomainException>(() =>
            Place.Create("somewhere", PlaceType.Museum, latitude, longitude, provinceId: 6));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(82)]
    public void Create_rejects_unknown_province(int provinceId)
    {
        Assert.Throws<DomainException>(() =>
            Place.Create("somewhere", PlaceType.Museum, 39.9, 32.8, provinceId));
    }

    [Fact]
    public void SetTranslation_replaces_existing_text_for_the_same_language()
    {
        var place = Place.Create("troy", PlaceType.ArchaeologicalSite, 39.9575, 26.2389, 17);

        place.SetTranslation(LanguageCode.English, "Troy", "Old text");
        place.SetTranslation(LanguageCode.English, "Troy", "New text");

        var translation = Assert.Single(place.Translations);
        Assert.Equal("New text", translation.Description);
    }

    [Fact]
    public void SetTranslation_rejects_unsupported_language()
    {
        var place = Place.Create("troy", PlaceType.ArchaeologicalSite, 39.9575, 26.2389, 17);

        Assert.Throws<DomainException>(() => place.SetTranslation("de", "Troja", ""));
    }

    [Fact]
    public void GetTranslation_falls_back_to_turkish()
    {
        var place = Place.Create("anitkabir", PlaceType.Monument, 39.9250, 32.8369, 6);
        place.SetTranslation(LanguageCode.Turkish, "Anıtkabir", "Atatürk'ün anıt mezarı.");

        var translation = place.GetTranslation(LanguageCode.English);

        Assert.NotNull(translation);
        Assert.Equal(LanguageCode.Turkish, translation.LanguageCode);
    }
}
