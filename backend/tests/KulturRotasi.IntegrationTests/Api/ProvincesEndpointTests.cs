using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KulturRotasi.Application.Places;
using KulturRotasi.Application.Provinces;
using KulturRotasi.IntegrationTests.Infrastructure;

namespace KulturRotasi.IntegrationTests.Api;

[Collection(ApiTestGroup.Name)]
public sealed class ProvincesEndpointTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Lists_all_provinces_with_a_place_count()
    {
        var provinces = (await _client.GetFromJsonAsync<List<ProvinceDto>>("/api/provinces", JsonDefaults.Options))!;

        Assert.Equal(81, provinces.Count);
        Assert.Equal(Enumerable.Range(1, 81), provinces.Select(p => p.PlateCode));
        Assert.True(provinces.Single(p => p.PlateCode == 6).PlaceCount >= 3);
    }

    [Fact]
    public async Task Places_in_a_province_are_found_by_point_in_polygon()
    {
        var izmir = (await _client.GetFromJsonAsync<List<PlaceSummaryDto>>("/api/provinces/35/places", JsonDefaults.Options))!;
        var provinces = (await _client.GetFromJsonAsync<List<ProvinceDto>>("/api/provinces", JsonDefaults.Options))!;

        Assert.Equal(["ephesus", "pergamon"], izmir.Select(p => p.Slug).Order());
        Assert.Equal(izmir.Count, provinces.Single(p => p.PlateCode == 35).PlaceCount);
    }

    [Fact]
    public async Task Unknown_province_returns_404()
    {
        var response = await _client.GetAsync("/api/provinces/99/places");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GeoJson_returns_simplified_province_polygons()
    {
        var simplified = await _client.GetStringAsync("/api/provinces/geojson");
        var full = await _client.GetStringAsync("/api/provinces/geojson?toleranceDegrees=0");
        using var json = JsonDocument.Parse(simplified);

        var features = json.RootElement.GetProperty("features");
        Assert.Equal(81, features.GetArrayLength());
        Assert.Matches("^(Multi)?Polygon$", features[0].GetProperty("geometry").GetProperty("type").GetString());
        Assert.True(features[0].GetProperty("properties").TryGetProperty("placeCount", out _));
        Assert.True(simplified.Length < full.Length / 2, $"Simplified {simplified.Length} bytes, full {full.Length} bytes");
    }

    [Fact]
    public async Task OpenApi_document_describes_the_endpoints()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/places/nearby", document, StringComparison.Ordinal);
        Assert.Contains("/api/provinces/{plateCode}/places", document, StringComparison.Ordinal);
    }
}
