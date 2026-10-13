using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MultiGuide.Data.Context;
using MultiGuide.Data.Seed;
using Xunit;

namespace MultiGuide.Architecture.Tests;

public class DbSeederTests
{
    private MultiGuideDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new MultiGuideDbContext(options);
    }

    [Fact]
    public async Task DbSeeder_WhenSeedDisabled_DoesNotInsertData()
    {
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Seed:Enabled", "false" }
            })
            .Build();

        await DbSeeder.SeedDemoDataAsync(context, config);

        Assert.Equal(0, await context.Languages.CountAsync());
        Assert.Equal(0, await context.TourAreas.CountAsync());
        Assert.Equal(0, await context.Pois.CountAsync());
        Assert.Equal(0, await context.PoiTranslations.CountAsync());
    }

    [Fact]
    public async Task DbSeeder_WhenSeedEnabled_InsertsOneAreaSixPoisAndEighteenTranslations()
    {
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Seed:Enabled", "true" }
            })
            .Build();

        await DbSeeder.SeedDemoDataAsync(context, config);

        // Verify Languages
        var languages = await context.Languages.ToListAsync();
        Assert.Equal(3, languages.Count);
        Assert.Contains(languages, l => l.Code == "vi");
        Assert.Contains(languages, l => l.Code == "en");
        Assert.Contains(languages, l => l.Code == "ja");

        // Verify Area
        var area = await context.TourAreas.FirstOrDefaultAsync(a => a.Code == "van-mieu");
        Assert.NotNull(area);
        Assert.Equal("Văn Miếu - Quốc Tử Giám", area.Name);
        Assert.Equal(5.00m, area.PriceUsd);
        Assert.Equal(24, area.PassTtlHours);

        // Verify POIs
        var pois = await context.Pois.Where(p => p.AreaId == area.Id).ToListAsync();
        Assert.Equal(6, pois.Count);

        // Verify Translations
        var translations = await context.PoiTranslations.ToListAsync();
        Assert.Equal(18, translations.Count);

        // Every POI must have exactly 3 translations (vi, en, ja)
        foreach (var poi in pois)
        {
            var poiTrans = translations.Where(t => t.PoiId == poi.Id).ToList();
            Assert.Equal(3, poiTrans.Count);
            Assert.Contains(poiTrans, t => t.LanguageCode == "vi");
            Assert.Contains(poiTrans, t => t.LanguageCode == "en");
            Assert.Contains(poiTrans, t => t.LanguageCode == "ja");
        }
    }

    [Fact]
    public async Task DbSeeder_WhenExecutedMultipleTimes_IsIdempotent()
    {
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Seed:Enabled", "true" }
            })
            .Build();

        // Run twice
        await DbSeeder.SeedDemoDataAsync(context, config);
        await DbSeeder.SeedDemoDataAsync(context, config);

        Assert.Equal(3, await context.Languages.CountAsync());
        Assert.Equal(1, await context.TourAreas.CountAsync());
        Assert.Equal(6, await context.Pois.CountAsync());
        Assert.Equal(18, await context.PoiTranslations.CountAsync());
    }

    [Fact]
    public async Task DbSeeder_PoisHaveValidCoordinatesAndRadius()
    {
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());

        await DbSeeder.SeedDemoDataAsync(context, force: true);

        var pois = await context.Pois.ToListAsync();
        Assert.Equal(6, pois.Count);

        foreach (var poi in pois)
        {
            Assert.InRange(poi.Lat, -90, 90);
            Assert.InRange(poi.Lng, -180, 180);
            Assert.InRange(poi.TriggerRadiusM, 5, 200);
            Assert.InRange(poi.Priority, 0, 100);
            Assert.True(poi.IsActive);
        }
    }
}
