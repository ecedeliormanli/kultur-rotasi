using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KulturRotasi.Application.Common;
using KulturRotasi.Application.Places;
using KulturRotasi.IntegrationTests.Infrastructure;

namespace KulturRotasi.IntegrationTests.Api;

/// <summary>Calls the places endpoints over HTTP against the seeded SQL Server.</summary>
[Collection(ApiTestGroup.Name)]
public sealed class PlacesEndpointTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Search_filters_by_type()
    {
        var page = await GetAsync<PagedResult<PlaceSummaryDto>>("/api/places?type=Palace");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(["ishak-pasha-palace", "topkapi-palace"], page.Items.Select(p => p.Slug).Order());
    }

    [Fact]
    public async Task Search_returns_pages()
    {
        var page = await GetAsync<PagedResult<PlaceSummaryDto>>("/api/places?page=2&pageSize=15");

        Assert.Equal(40, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(15, page.Items.Count);
    }

    [Fact]
    public async Task Search_finds_a_place_by_its_turkish_or_english_name()
    {
        var turkish = await GetAsync<PagedResult<PlaceSummaryDto>>("/api/places?search=efes");
        var english = await GetAsync<PagedResult<PlaceSummaryDto>>("/api/places?search=ephesus");

        Assert.Equal("ephesus", Assert.Single(turkish.Items).Slug);
        Assert.Equal("ephesus", Assert.Single(english.Items).Slug);
    }

    [Fact]
    public async Task Names_follow_the_accept_language_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/places/topkapi-palace");
        request.Headers.AcceptLanguage.ParseAdd("en-GB");
        var english = await SendAsync<PlaceDetailsDto>(request);

        var turkish = await GetAsync<PlaceDetailsDto>("/api/places/topkapi-palace");

        Assert.Equal(("en", "Topkapı Palace"), (english.Language, english.Name));
        Assert.Equal(("tr", "Topkapı Sarayı"), (turkish.Language, turkish.Name));
        Assert.False(string.IsNullOrWhiteSpace(english.Description));
    }

    [Fact]
    public async Task Unknown_slug_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/places/atlantis");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Nearby_returns_the_closest_places_first()
    {
        // Kızılay square, Ankara
        var places = await GetAsync<List<NearbyPlaceDto>>("/api/places/nearby?latitude=39.9208&longitude=32.8541&limit=3");

        Assert.Equal(
            ["anitkabir", "museum-of-anatolian-civilizations", "erimtan-archaeology-and-arts-museum"],
            places.Select(p => p.Slug));
        Assert.All(places, p => Assert.InRange(p.DistanceKm, 0.5, 3));
        Assert.Equal(places.Select(p => p.DistanceKm).Order(), places.Select(p => p.DistanceKm));
    }

    [Fact]
    public async Task Nearby_with_a_radius_leaves_out_places_further_away()
    {
        var places = await GetAsync<List<NearbyPlaceDto>>("/api/places/nearby?latitude=37.9395&longitude=27.3417&radiusKm=50&limit=100");

        Assert.Contains(places, p => p.Slug == "ephesus");
        Assert.DoesNotContain(places, p => p.Slug == "pergamon");
        Assert.All(places, p => Assert.True(p.DistanceKm <= 50));
    }

    [Fact]
    public async Task Nearby_rejects_a_radius_that_is_too_large()
    {
        var response = await _client.GetAsync("/api/places/nearby?latitude=39.92&longitude=32.85&radiusKm=1000");

        var errors = await ReadValidationErrorsAsync(response);
        Assert.Contains("radiusKm", errors.Keys);
    }

    [Fact]
    public async Task Nearby_requires_coordinates()
    {
        var response = await _client.GetAsync("/api/places/nearby?longitude=32.85");

        var errors = await ReadValidationErrorsAsync(response);
        Assert.Contains("latitude", errors.Keys);
    }

    [Theory]
    [InlineData("[[30.4,36.7],[31.5,36.7],[31.5,37.1],[30.4,37.1],[30.4,36.7]]")] // counter-clockwise
    [InlineData("[[30.4,36.7],[30.4,37.1],[31.5,37.1],[31.5,36.7],[30.4,36.7]]")] // clockwise
    public async Task Within_returns_the_places_inside_a_polygon_in_either_direction(string ring)
    {
        var body = $$"""{ "geometry": { "type": "Polygon", "coordinates": [{{ring}}] } }""";

        var places = await PostAsync<List<PlaceSummaryDto>>("/api/places/within", body);

        Assert.Equal(
            ["antalya-museum", "aspendos", "perge", "side", "termessos"],
            places.Select(p => p.Slug).Order());
    }

    [Fact]
    public async Task Within_a_buffer_around_a_line_finds_places_along_the_coast()
    {
        // A line along latitude 36.3 on the Lycian coast, with a 15 km buffer
        const string body = """{ "geometry": { "type": "LineString", "coordinates": [[29.0, 36.3], [30.6, 36.3]] }, "bufferKm": 15 }""";

        var places = await PostAsync<List<PlaceSummaryDto>>("/api/places/within", body);

        Assert.Equal(["myra", "olympos", "patara", "xanthos"], places.Select(p => p.Slug).Order());
    }

    [Fact]
    public async Task Within_rejects_a_point_without_a_buffer()
    {
        using var content = JsonContent("""{ "geometry": { "type": "Point", "coordinates": [27.34, 37.94] } }""");
        var response = await _client.PostAsync("/api/places/within", content);

        var errors = await ReadValidationErrorsAsync(response);
        Assert.Contains("bufferKm", errors.Keys);
    }

    [Fact]
    public async Task GeoJson_returns_every_place_as_a_point_feature()
    {
        var response = await _client.GetAsync("/api/places/geojson");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("application/geo+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("FeatureCollection", json.RootElement.GetProperty("type").GetString());
        var features = json.RootElement.GetProperty("features");
        Assert.Equal(40, features.GetArrayLength());
        Assert.Equal("Point", features[0].GetProperty("geometry").GetProperty("type").GetString());
        Assert.True(features[0].GetProperty("properties").TryGetProperty("name", out _));
    }

    private async Task<T> GetAsync<T>(string url) =>
        (await _client.GetFromJsonAsync<T>(url, JsonDefaults.Options))!;

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Options))!;
    }

    private async Task<T> PostAsync<T>(string url, string json)
    {
        using var content = JsonContent(json);
        var response = await _client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Options))!;
    }

    private static StringContent JsonContent(string json) => new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task<Dictionary<string, string[]>> ReadValidationErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
    }
}
