using System.Text.Json;
using KulturRotasi.Api.GeoFormats;
using KulturRotasi.Api.Infrastructure;
using KulturRotasi.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Api.Controllers;

/// <summary>Reads GeoJSON or KML files so they can be shown on the map. Nothing is saved.</summary>
/// <remarks>Saving imported places to the database comes with the admin role (M6).</remarks>
[ApiController]
[Route("api/import")]
public sealed class ImportController(IOptions<JsonOptions> jsonOptions) : ControllerBase
{
    public const long MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxFeatures = 5_000;

    /// <summary>Converts an uploaded .geojson or .kml file to a GeoJSON FeatureCollection.</summary>
    /// <param name="file">A GeoJSON (.geojson, .json) or KML (.kml) file, at most 2 MB.</param>
    [HttpPost("preview")]
    [RequestSizeLimit(MaxFileBytes + 64 * 1024)]
    [Consumes("multipart/form-data")]
    [Produces(GeoJsonMapper.ContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Preview(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is 0 or > MaxFileBytes)
        {
            throw new DomainException($"The file must be between 1 byte and {MaxFileBytes / 1024 / 1024} MB.");
        }

        await using var stream = file.OpenReadStream();
        var features = Path.GetExtension(file.FileName).ToUpperInvariant() switch
        {
            ".KML" => KmlReader.Read(stream),
            ".GEOJSON" or ".JSON" => await ReadGeoJsonAsync(stream, cancellationToken),
            _ => throw new DomainException("Only .geojson, .json and .kml files are supported."),
        };

        if (features.Count == 0 || features.Count > MaxFeatures)
        {
            throw new DomainException($"The file must contain between 1 and {MaxFeatures} features.");
        }

        foreach (var feature in features)
        {
            feature.Geometry.SRID = Geo.Wgs84;
            if (!feature.Geometry.Coordinates.All(c => c.Y is >= -90 and <= 90 && c.X is >= -180 and <= 180))
            {
                throw new DomainException("Coordinates must be longitude/latitude in WGS 84.");
            }
        }

        return Ok(features);
    }

    private async Task<FeatureCollection> ReadGeoJsonAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var options = jsonOptions.Value.JsonSerializerOptions;

            // Accept a FeatureCollection, a single Feature, or a bare geometry
            return document.RootElement.GetProperty("type").GetString() switch
            {
                "FeatureCollection" => document.RootElement.Deserialize<FeatureCollection>(options)!,
                "Feature" => [document.RootElement.Deserialize<IFeature>(options)!],
                _ => [new Feature(document.RootElement.Deserialize<Geometry>(options)!, new AttributesTable())],
            };
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            throw new DomainException("The file is not valid GeoJSON.");
        }
    }
}
