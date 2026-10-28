using KulturRotasi.Application;
using KulturRotasi.Infrastructure;
using KulturRotasi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateAndSeedOnStartup"))
{
    await app.Services.MigrateAndSeedAsync();
}

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json, interactive UI at /swagger
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Kültür Rotası API v1"));
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
