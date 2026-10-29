using KulturRotasi.Domain.Common;
using KulturRotasi.Domain.Provinces;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Domain.Places;

/// <summary>A museum, archaeological site or monument that can be shown on the map and added to routes.</summary>
public sealed class Place
{
    public const int SlugMaxLength = 100;

    private readonly List<PlaceTranslation> _translations = [];

    public int Id { get; private set; }

    /// <summary>URL-friendly unique name, e.g. "ephesus".</summary>
    public string Slug { get; private set; } = null!;

    public PlaceType Type { get; private set; }

    /// <summary>Position in WGS 84. Stored as a SQL Server <c>geography</c> point.</summary>
    public Point Location { get; private set; } = null!;

    public int ProvinceId { get; private set; }
    public Province Province { get; private set; } = null!;

    public bool IsUnescoWorldHeritage { get; private set; }

    public IReadOnlyCollection<PlaceTranslation> Translations => _translations.AsReadOnly();

    public double Latitude => Location.Y;
    public double Longitude => Location.X;

    private Place()
    {
        // Required by EF Core
    }

    public static Place Create(
        string slug,
        PlaceType type,
        double latitude,
        double longitude,
        int provinceId,
        bool isUnescoWorldHeritage = false)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > SlugMaxLength)
        {
            throw new DomainException($"Slug is required and must be at most {SlugMaxLength} characters.");
        }

        if (!Province.IsValidPlateCode(provinceId))
        {
            throw new DomainException($"Province plate code must be between 1 and 81, got {provinceId}.");
        }

        return new Place
        {
            Slug = slug.Trim().ToLowerInvariant(),
            Type = type,
            Location = Geo.CreatePoint(latitude, longitude),
            ProvinceId = provinceId,
            IsUnescoWorldHeritage = isUnescoWorldHeritage,
        };
    }

    /// <summary>Adds the text for a language, or replaces it if that language already exists.</summary>
    public void SetTranslation(string languageCode, string name, string description)
    {
        if (!LanguageCode.IsSupported(languageCode))
        {
            throw new DomainException($"Language '{languageCode}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > PlaceTranslation.NameMaxLength)
        {
            throw new DomainException($"Name is required and must be at most {PlaceTranslation.NameMaxLength} characters.");
        }

        if (description.Length > PlaceTranslation.DescriptionMaxLength)
        {
            throw new DomainException($"Description must be at most {PlaceTranslation.DescriptionMaxLength} characters.");
        }

        var existing = _translations.Find(t => t.LanguageCode == languageCode);
        if (existing is null)
        {
            _translations.Add(new PlaceTranslation(languageCode, name.Trim(), description.Trim()));
        }
        else
        {
            existing.Update(name.Trim(), description.Trim());
        }
    }

    /// <summary>Text in the requested language, falling back to Turkish.</summary>
    public PlaceTranslation? GetTranslation(string languageCode) =>
        _translations.Find(t => t.LanguageCode == languageCode)
        ?? _translations.Find(t => t.LanguageCode == LanguageCode.Turkish);
}
