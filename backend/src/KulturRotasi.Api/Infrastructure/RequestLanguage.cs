using System.Globalization;

namespace KulturRotasi.Api.Infrastructure;

/// <summary>
/// The content language of the current request. Set by the request localization middleware from
/// <c>?culture=en</c> or the <c>Accept-Language</c> header; Turkish when neither is given.
/// </summary>
internal static class RequestLanguage
{
    public static string Current => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
}
