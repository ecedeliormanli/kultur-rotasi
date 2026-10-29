namespace KulturRotasi.Domain.Places;

/// <summary>Name and description of a place in one language.</summary>
public sealed class PlaceTranslation
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public int PlaceId { get; private set; }
    public string LanguageCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private PlaceTranslation()
    {
        // Required by EF Core
    }

    internal PlaceTranslation(string languageCode, string name, string description)
    {
        LanguageCode = languageCode;
        Name = name;
        Description = description;
    }

    internal void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
