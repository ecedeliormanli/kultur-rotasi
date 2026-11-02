using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace KulturRotasi.Api.Infrastructure;

/// <summary>
/// Enums are sent as names ("Museum"), but the generated schema lists only the names without saying
/// they are strings. Swagger UI then cannot show them as a drop-down, so the type is added here.
/// </summary>
internal static class EnumSchemaTransformer
{
    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
        }

        return Task.CompletedTask;
    }
}
