using System.Globalization;
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
    .AddJsonOptions(options =>
    {
        // Enums as "Museum" instead of 1; NetTopologySuite geometries as GeoJSON
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory(Geo.Factory));
    });

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
