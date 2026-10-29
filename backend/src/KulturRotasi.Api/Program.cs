using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using KulturRotasi.Api.Infrastructure;
using KulturRotasi.Application;
using KulturRotasi.Domain.Common;
using KulturRotasi.Infrastructure;
using KulturRotasi.Infrastructure.Persistence;
using NetTopologySuite.IO.Converters;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

// The OpenAPI generator reads these options (not the MVC ones above), so both get the same settings
builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemExceptionHandler>();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = LanguageCode.Supported.Select(code => new CultureInfo(code)).ToList();
    options.DefaultRequestCulture = new(LanguageCode.Turkish);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
});

builder.Services.AddOpenApi(options => options.AddSchemaTransformer(GeoJsonSchemaTransformer.TransformAsync));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup"))
{
    await app.Services.MigrateAndSeedAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json, interactive UI at /swagger
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Kültür Rotası API v1"));
}

app.UseRequestLocalization();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

static void ConfigureJson(JsonSerializerOptions options)
{
    // Numbers must be JSON numbers ("web" defaults also accept "15" as a string, which confuses Swagger UI)
    options.NumberHandling = JsonNumberHandling.Strict;
    // Enums as "Museum" instead of 1; NetTopologySuite geometries as GeoJSON
    options.Converters.Add(new JsonStringEnumConverter());
    options.Converters.Add(new GeoJsonConverterFactory(Geo.Factory));
}
