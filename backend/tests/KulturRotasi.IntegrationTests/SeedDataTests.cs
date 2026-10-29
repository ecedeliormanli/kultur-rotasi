using KulturRotasi.Domain.Common;
using KulturRotasi.Infrastructure.Persistence;
using KulturRotasi.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KulturRotasi.IntegrationTests;

/// <summary>Checks the seeded data and runs the first spatial queries against SQL Server.</summary>
[Collection(ApiTestGroup.Name)]
public sealed class SeedDataTests(ApiFactory factory) : IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();
    private AppDbContext Db => _scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task All_81_provinces_and_the_sample_places_are_seeded()
    {
        Assert.Equal(81, await Db.Provinces.CountAsync());
        Assert.Equal(40, await Db.Places.CountAsync());
    }

    [Fact]
    public async Task Every_place_has_turkish_and_english_text()
    {
        var places = await Db.Places.ToListAsync();

        Assert.All(places, place =>
        {
            Assert.NotNull(place.Translations.SingleOrDefault(t => t.LanguageCode == LanguageCode.Turkish));
            Assert.NotNull(place.Translations.SingleOrDefault(t => t.LanguageCode == LanguageCode.English));
        });
    }

    [Fact]
    public async Task Every_place_lies_within_its_province()
    {
        // Point-in-polygon check done by SQL Server (STDistance on geography, in metres).
        // A small tolerance allows for simplified coastlines in the boundary data.
        const double toleranceMetres = 2_000;

        var outliers = await Db.Places
            .Where(p => p.Province.Boundary.Distance(p.Location) > toleranceMetres)
            .Select(p => p.Slug)
            .ToListAsync();

        Assert.Empty(outliers);
    }

    [Fact]
    public async Task Ankara_contains_the_museum_of_anatolian_civilizations()
    {
        var ankara = await Db.Provinces.SingleAsync(p => p.Id == 6);

        var slugs = await Db.Places
            .Where(p => ankara.Boundary.Contains(p.Location))
            .Select(p => p.Slug)
            .ToListAsync();

        Assert.Contains("museum-of-anatolian-civilizations", slugs);
        Assert.DoesNotContain("ephesus", slugs);
    }

    [Fact]
    public async Task Distance_between_two_places_is_returned_in_metres()
    {
        var ephesus = await Db.Places.SingleAsync(p => p.Slug == "ephesus");

        var distanceToPergamon = await Db.Places
            .Where(p => p.Slug == "pergamon")
            .Select(p => p.Location.Distance(ephesus.Location))
            .SingleAsync();

        // Straight-line distance Ephesus–Pergamon is roughly 135 km
        Assert.InRange(distanceToPergamon, 120_000, 150_000);
    }

    public void Dispose() => _scope.Dispose();
}
