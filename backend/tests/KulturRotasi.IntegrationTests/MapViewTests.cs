using KulturRotasi.Infrastructure.Persistence;
using KulturRotasi.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KulturRotasi.IntegrationTests;

/// <summary>The SQL views that GeoServer publishes as WMS/WFS layers.</summary>
[Collection(ApiTestGroup.Name)]
public sealed class MapViewTests(ApiFactory factory) : IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();
    private AppDbContext Db => _scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task MapPlaces_has_one_row_per_place_with_both_names_and_a_geometry()
    {
        var rows = await Db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.MapPlaces WHERE NameTr IS NOT NULL AND NameEn IS NOT NULL AND Geom.STSrid = 4326")
            .SingleAsync();

        Assert.Equal(40, rows);
    }

    [Fact]
    public async Task MapProvinces_counts_places_inside_each_border()
    {
        var antalyaCount = await Db.Database
            .SqlQuery<int>($"SELECT PlaceCount AS Value FROM dbo.MapProvinces WHERE PlateCode = 7")
            .SingleAsync();
        var total = await Db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.MapProvinces WHERE Geom.STIsValid() = 1")
            .SingleAsync();

        Assert.True(antalyaCount >= 7, $"Antalya has {antalyaCount} places");
        Assert.Equal(81, total);
    }

    public void Dispose() => _scope.Dispose();
}
