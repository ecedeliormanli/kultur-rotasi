using System.Globalization;
using System.Reflection;
using System.Text.Json;
using KulturRotasi.Domain.Places;
using KulturRotasi.Domain.Provinces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Converters;

namespace KulturRotasi.Infrastructure.Persistence.Seed;

/// <summary>
/// Fills an empty database with the 81 provinces (with borders) and the sample places.
/// The data files are embedded in this assembly, so seeding works the same locally, in tests and in Docker.
/// </summary>
public sealed partial class DataSeeder(AppDbContext db, ILogger<DataSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new GeoJsonConverterFactory() },
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await db.Provinces.AnyAsync(cancellationToken))
        {
            var provinces = await LoadProvincesAsync(cancellationToken);
            db.Provinces.AddRange(provinces);
            await db.SaveChangesAsync(cancellationToken);
            LogSeeded(provinces.Count, "provinces");
        }

        if (!await db.Places.AnyAsync(cancellationToken))
        {
            var places = await LoadPlacesAsync(cancellationToken);
            db.Places.AddRange(places);
            await db.SaveChangesAsync(cancellationToken);
            LogSeeded(places.Count, "places");
        }
    }

    private static async Task<List<Province>> LoadProvincesAsync(CancellationToken cancellationToken)
    {
        await using var stream = OpenResource("provinces.geojson");
        var collection = await JsonSerializer.DeserializeAsync<FeatureCollection>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("provinces.geojson is empty.");

        return collection
            .Select(feature => new Province(
                plateCode: Convert.ToInt32(feature.Attributes["plateCode"], CultureInfo.InvariantCulture),
                name: (string)feature.Attributes["name"],
                boundary: GeographyFixer.Fix(feature.Geometry)))
            .ToList();
    }

    private static async Task<List<Place>> LoadPlacesAsync(CancellationToken cancellationToken)
    {
        await using var stream = OpenResource("places.json");
        var file = await JsonSerializer.DeserializeAsync<PlacesFile>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("places.json is empty.");

        return file.Places.Select(item =>
        {
            var place = Place.Create(
                item.Slug,
                Enum.Parse<PlaceType>(item.Type),
                item.Latitude,
                item.Longitude,
                item.ProvincePlateCode,
                item.IsUnescoWorldHeritage);

            foreach (var (language, text) in item.Translations)
            {
                place.SetTranslation(language, text.Name, text.Description);
            }

            return place;
        }).ToList();
    }

    private static Stream OpenResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
        return assembly.GetManifestResourceStream(resourceName)!;
    }

    // Source-generated logging: the message template is parsed at compile time, not on every call
    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} {Entity}")]
    private partial void LogSeeded(int count, string entity);

    private sealed record PlacesFile(List<PlaceItem> Places);

    private sealed record PlaceItem(
        string Slug,
        string Type,
        double Latitude,
        double Longitude,
        int ProvincePlateCode,
        bool IsUnescoWorldHeritage,
        Dictionary<string, TranslationItem> Translations);

    private sealed record TranslationItem(string Name, string Description);
}
