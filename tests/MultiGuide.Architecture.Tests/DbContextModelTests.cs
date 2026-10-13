using Microsoft.EntityFrameworkCore;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;
using Xunit;

namespace MultiGuide.Architecture.Tests;

public class DbContextModelTests
{
    private readonly MultiGuideDbContext _context;
    public DbContextModelTests()
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseNpgsql("Host=localhost;Database=test_multiguide;Username=postgres;Password=postgres")
            .Options;

        _context = new MultiGuideDbContext(options);
    }

    [Fact]
    public void Model_Should_Contain_All_Seven_Entities()
    {
        var entityTypes = _context.Model.GetEntityTypes().Select(e => e.ClrType).ToList();

        Assert.Contains(typeof(Language), entityTypes);
        Assert.Contains(typeof(TourArea), entityTypes);
        Assert.Contains(typeof(Poi), entityTypes);
        Assert.Contains(typeof(PoiTranslation), entityTypes);
        Assert.Contains(typeof(Payment), entityTypes);
        Assert.Contains(typeof(AccessPass), entityTypes);
        Assert.Contains(typeof(PlayLog), entityTypes);
    }

    [Fact]
    public void Tables_Should_Be_Configured_With_Exact_ERD_Names()
    {
        Assert.Equal("language", _context.Model.FindEntityType(typeof(Language))?.GetTableName());
        Assert.Equal("tour_area", _context.Model.FindEntityType(typeof(TourArea))?.GetTableName());
        Assert.Equal("poi", _context.Model.FindEntityType(typeof(Poi))?.GetTableName());
        Assert.Equal("poi_translation", _context.Model.FindEntityType(typeof(PoiTranslation))?.GetTableName());
        Assert.Equal("payment", _context.Model.FindEntityType(typeof(Payment))?.GetTableName());
        Assert.Equal("access_pass", _context.Model.FindEntityType(typeof(AccessPass))?.GetTableName());
        Assert.Equal("play_log", _context.Model.FindEntityType(typeof(PlayLog))?.GetTableName());
    }

    [Fact]
    public void ForeignKeys_Should_Have_Restrict_DeleteBehavior()
    {
        var foreignKeys = _context.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys());

        foreach (var fk in foreignKeys)
        {
            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        }
    }

    [Fact]
    public void UniqueConstraints_And_Indexes_Should_Match_ERD()
    {
        var tourArea = _context.Model.FindEntityType(typeof(TourArea))!;
        var codeIndex = tourArea.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(TourArea.Code)));
        Assert.NotNull(codeIndex);
        Assert.True(codeIndex.IsUnique);

        var payment = _context.Model.FindEntityType(typeof(Payment))!;
        var paymentKeyIndex = payment.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Payment.IdempotencyKey)));
        Assert.NotNull(paymentKeyIndex);
        Assert.True(paymentKeyIndex.IsUnique);

        var accessPass = _context.Model.FindEntityType(typeof(AccessPass))!;
        var tokenHashIndex = accessPass.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(AccessPass.TokenHash)));
        Assert.NotNull(tokenHashIndex);
        Assert.True(tokenHashIndex.IsUnique);

        var poiTranslation = _context.Model.FindEntityType(typeof(PoiTranslation))!;
        var poiLangIndex = poiTranslation.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == nameof(PoiTranslation.PoiId)) &&
            i.Properties.Any(p => p.Name == nameof(PoiTranslation.LanguageCode)));
        Assert.NotNull(poiLangIndex);
        Assert.True(poiLangIndex.IsUnique);

        var poi = _context.Model.FindEntityType(typeof(Poi))!;
        var poiAreaActiveIndex = poi.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == nameof(Poi.AreaId)) &&
            i.Properties.Any(p => p.Name == nameof(Poi.IsActive)));
        Assert.NotNull(poiAreaActiveIndex);
        Assert.Equal("idx_poi_area_active", poiAreaActiveIndex.GetDatabaseName());

        var poiLocationIndex = poi.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == nameof(Poi.Lat)) &&
            i.Properties.Any(p => p.Name == nameof(Poi.Lng)));
        Assert.NotNull(poiLocationIndex);
        Assert.Equal("idx_poi_location", poiLocationIndex.GetDatabaseName());

        var playLog = _context.Model.FindEntityType(typeof(PlayLog))!;
        var playLogIndex = playLog.GetIndexes().FirstOrDefault(i =>
            i.Properties.Any(p => p.Name == nameof(PlayLog.PoiId)) &&
            i.Properties.Any(p => p.Name == nameof(PlayLog.StartedAt)));
        Assert.NotNull(playLogIndex);
        Assert.Equal("idx_playlog_poi_started", playLogIndex.GetDatabaseName());
    }
    [Fact]
    public void Factory_Should_Parse_Postgres_Uri_Correctly()
    {
        var uri = "postgresql://postgres.project:secret_password@aws-0-ap-southeast-1.pooler.supabase.com:6543/postgres";
        var connStr = MultiGuideDbContextFactory.ConvertPostgresUriToConnectionString(uri);

        Assert.Contains("Host=aws-0-ap-southeast-1.pooler.supabase.com", connStr);
        Assert.Contains("Port=6543", connStr);
        Assert.Contains("Database=postgres", connStr);
        Assert.Contains("Username=postgres.project", connStr);
        Assert.Contains("Password=secret_password", connStr);
    }
}
