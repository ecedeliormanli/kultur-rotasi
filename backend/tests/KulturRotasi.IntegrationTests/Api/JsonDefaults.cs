using System.Text.Json;
using System.Text.Json.Serialization;

namespace KulturRotasi.IntegrationTests.Api;

internal static class JsonDefaults
{
    /// <summary>Same settings as the API: camelCase names and enums as strings.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
