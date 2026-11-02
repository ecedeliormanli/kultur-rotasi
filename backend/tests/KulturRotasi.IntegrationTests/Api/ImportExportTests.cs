using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using KulturRotasi.IntegrationTests.Infrastructure;

namespace KulturRotasi.IntegrationTests.Api;

/// <summary>KML export and GeoJSON/KML file preview.</summary>
[Collection(ApiTestGroup.Name)]
public sealed class ImportExportTests(ApiFactory factory)
{
    private static readonly XNamespace Kml = "http://www.opengis.net/kml/2.2";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Kml_export_contains_a_placemark_per_place()
    {
        var response = await _client.GetAsync("/api/places/kml?type=Palace");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/vnd.google-earth.kml+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("kultur-rotasi.kml", response.Content.Headers.ContentDisposition?.FileName);

        var kml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var placemarks = kml.Descendants(Kml + "Placemark").ToList();
        Assert.Equal(2, placemarks.Count);

        var topkapi = placemarks.Single(p => (string?)p.Attribute("id") == "topkapi-palace");
        Assert.Equal("Topkapı Sarayı", topkapi.Element(Kml + "name")?.Value);
        Assert.Equal("28.9834,41.0115", topkapi.Descendants(Kml + "coordinates").Single().Value);
    }

    [Fact]
    public async Task Kml_file_is_converted_to_geojson()
    {
        const string kml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <Placemark>
                  <name>Likya Yolu</name>
                  <LineString><coordinates>29.0,36.3,0 30.6,36.3,0</coordinates></LineString>
                </Placemark>
                <Placemark>
                  <name>Antalya</name>
                  <Polygon><outerBoundaryIs><LinearRing>
                    <coordinates>30.4,36.7 31.5,36.7 31.5,37.1 30.4,37.1 30.4,36.7</coordinates>
                  </LinearRing></outerBoundaryIs></Polygon>
                </Placemark>
              </Document>
            </kml>
            """;

        using var json = await PreviewAsync("route.kml", kml);

        var features = json.RootElement.GetProperty("features");
        Assert.Equal(2, features.GetArrayLength());
        Assert.Equal("LineString", features[0].GetProperty("geometry").GetProperty("type").GetString());
        Assert.Equal("Likya Yolu", features[0].GetProperty("properties").GetProperty("name").GetString());
        Assert.Equal("Polygon", features[1].GetProperty("geometry").GetProperty("type").GetString());
    }

    [Fact]
    public async Task Geojson_file_is_returned_as_a_feature_collection()
    {
        const string geoJson = """{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [27.34, 37.94] }, "properties": { "name": "Efes" } }""";

        using var json = await PreviewAsync("point.geojson", geoJson);

        Assert.Equal("FeatureCollection", json.RootElement.GetProperty("type").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("features").GetArrayLength());
    }

    [Fact]
    public async Task Kml_with_an_external_entity_is_rejected()
    {
        // Classic XXE attack: the entity would make an unsafe parser read a local file into the response
        const string kml = """
            <?xml version="1.0"?>
            <!DOCTYPE kml [ <!ENTITY secret SYSTEM "file:///etc/passwd"> ]>
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Placemark><name>&secret;</name><Point><coordinates>27,37</coordinates></Point></Placemark>
            </kml>
            """;

        var response = await PostFileAsync("attack.kml", kml);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("root:", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("notes.txt", "hello")]
    [InlineData("broken.geojson", "{ not json")]
    [InlineData("empty.geojson", """{ "type": "FeatureCollection", "features": [] }""")]
    public async Task Unsupported_or_invalid_files_are_rejected(string fileName, string content)
    {
        var response = await PostFileAsync(fileName, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<JsonDocument> PreviewAsync(string fileName, string content)
    {
        var response = await PostFileAsync(fileName, content);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> PostFileAsync(string fileName, string content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        return await _client.PostAsync("/api/import/preview", form);
    }
}
