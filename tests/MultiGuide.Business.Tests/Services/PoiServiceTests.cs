using Moq;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Tests.Services;

public class PoiServiceTests
{
    private readonly Mock<IPoiRepository> _poiRepoMock;
    private readonly Mock<IGeoService> _geoServiceMock;
    private readonly PoiService _poiService;

    public PoiServiceTests()
    {
        _poiRepoMock = new Mock<IPoiRepository>();
        _geoServiceMock = new Mock<IGeoService>();
        _poiService = new PoiService(_poiRepoMock.Object, _geoServiceMock.Object);
    }

    [Fact]
    public async Task GetNearbyPoisAsync_DelegatesToRepositoryAndGeoService()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var lat = 21.0285;
        var lng = 105.8542;
        var radiusM = 50.0;
        var lang = "vi";
        using var cts = new CancellationTokenSource();

        var pois = new List<Poi>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AreaId = areaId,
                Lat = 21.0285,
                Lng = 105.8542,
                TriggerRadiusM = 30,
                IsActive = true
            }
        };

        var expectedResponses = new List<NearbyPoiResponse>
        {
            new()
            {
                Id = pois[0].Id,
                Title = "Cổng Văn Miếu",
                Lat = 21.0285,
                Lng = 105.8542,
                DistanceM = 5,
                WithinTrigger = true,
                TriggerRadiusM = 30,
                Priority = 1
            }
        };

        _poiRepoMock.Setup(r => r.GetActiveByAreaIdAsync(areaId, cts.Token))
            .ReturnsAsync(pois);

        _geoServiceMock.Setup(g => g.FindNearbyPois(pois, lat, lng, radiusM, lang))
            .Returns(expectedResponses);

        // Act
        var result = await _poiService.GetNearbyPoisAsync(areaId, lat, lng, radiusM, lang, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(expectedResponses[0].Id, result[0].Id);
        Assert.Equal(expectedResponses[0].Title, result[0].Title);

        _poiRepoMock.Verify(r => r.GetActiveByAreaIdAsync(areaId, cts.Token), Times.Once);
        _geoServiceMock.Verify(g => g.FindNearbyPois(pois, lat, lng, radiusM, lang), Times.Once);
    }

    [Fact]
    public async Task GetNearbyPoisAsync_WhenNoPoisInArea_ReturnsEmptyList()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var emptyPois = new List<Poi>();
        var emptyResponses = new List<NearbyPoiResponse>();

        _poiRepoMock.Setup(r => r.GetActiveByAreaIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPois);

        _geoServiceMock.Setup(g => g.FindNearbyPois(emptyPois, 21.0, 105.0, 50.0, null))
            .Returns(emptyResponses);

        // Act
        var result = await _poiService.GetNearbyPoisAsync(areaId, 21.0, 105.0, 50.0);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
