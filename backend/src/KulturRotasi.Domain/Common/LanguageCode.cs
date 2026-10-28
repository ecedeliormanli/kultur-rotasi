namespace KulturRotasi.Domain.Common;

/// <summary>Languages the content is available in (ISO 639-1 codes).</summary>
public static class LanguageCode
{
    public const string Turkish = "tr";
    public const string English = "en";

    public static readonly IReadOnlyList<string> Supported = [Turkish, English];

    public static bool IsSupported(string code) => Supported.Contains(code);
}
