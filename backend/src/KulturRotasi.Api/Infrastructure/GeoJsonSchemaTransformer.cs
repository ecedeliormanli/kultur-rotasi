using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NetTopologySuite.Geometries;

namespace KulturRotasi.Api.Infrastructure;

/// <summary>
/// NetTopologySuite geometries are read and written as GeoJSON by a custom JSON converter, but the OpenAPI
/// generator only sees the C# class and would document its internals (factory, envelope, precision model...).
/// This replaces that schema with what actually goes over the wire: a GeoJSON geometry object.
/// </summary>
internal static class GeoJsonSchemaTransformer
{
    private static readonly string[] GeometryTypes =
        ["Point", "LineString", "Polygon", "MultiPoint", "MultiLineString", "MultiPolygon"];

    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (!typeof(Geometry).IsAssignableFrom(context.JsonTypeInfo.Type))
        {
            return Task.CompletedTask;
        }

        schema.Type = JsonSchemaType.Object;
        schema.Description = "GeoJSON geometry (RFC 7946). Coordinates are [longitude, latitude] in WGS 84.";
        schema.Required = new HashSet<string> { "type", "coordinates" };
        schema.Properties = new Dictionary<string, IOpenApiSchema>
        {
            ["type"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = GeometryTypes.Select(name => (JsonNode)JsonValue.Create(name)).ToList(),
            },
            ["coordinates"] = new OpenApiSchema { Type = JsonSchemaType.Array },
        };
        schema.Examples = [JsonNode.Parse("""{ "type": "LineString", "coordinates": [[29.0, 36.3], [30.6, 36.3]] }""")!];
        schema.AdditionalPropertiesAllowed = true;
        return Task.CompletedTask;
    }
}
