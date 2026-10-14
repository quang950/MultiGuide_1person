using Moq;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class AdminContentServicePoiTests
{
    private readonly Mock<IAreaRepository> _areaRepoMock = new();
    private readonly Mock<IPoiRepository> _poiRepoMock = new();
    private readonly AdminContentService _service;

    public AdminContentServicePoiTests()
    {
        _service = new AdminContentService(_areaRepoMock.Object, _poiRepoMock.Object);
    }

    private static CreatePoiRequest ValidCreate(Guid areaId) => new()
    {
        AreaId = areaId,
        Lat = 21.0285,
        Lng = 105.8355,
        TriggerRadiusM = 30,
        Priority = 10,
        ImageUrl = "https://example.com/a.jpg"
    };

    [Fact]
    public async Task CreatePoiAsync_HopLe_TaoThanhCong()
    {
        var areaId = Guid.NewGuid();
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TourArea { Id = areaId, Code = "van-mieu" });
        _poiRepoMock.Setup(r => r.AddAsync(It.IsAny<Poi>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poi p, CancellationToken _) => p);

        var result = await _service.CreatePoiAsync(ValidCreate(areaId));

        Assert.Equal(areaId, result.AreaId);
        Assert.Equal(30, result.TriggerRadiusM);
        Assert.Equal(10, result.Priority);
        Assert.True(result.IsActive);
        _poiRepoMock.Verify(r => r.AddAsync(It.Is<Poi>(p => p.IsActive && p.AreaId == areaId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePoiAsync_ImageUrlRong_LuuNull()
    {
        var areaId = Guid.NewGuid();
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TourArea { Id = areaId });
        _poiRepoMock.Setup(r => r.AddAsync(It.IsAny<Poi>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poi p, CancellationToken _) => p);

        var request = ValidCreate(areaId);
        request.ImageUrl = "   ";

        var result = await _service.CreatePoiAsync(request);

        Assert.Null(result.ImageUrl);
    }

    [Fact]
    public async Task CreatePoiAsync_AreaKhongTonTai_NemNotFound_AreaNotFound()
    {
        var areaId = Guid.NewGuid();
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreatePoiAsync(ValidCreate(areaId)));

        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
        _poiRepoMock.Verify(r => r.AddAsync(It.IsAny<Poi>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(-90.01, 0, 30, 0)]
    [InlineData(90.01, 0, 30, 0)]
    [InlineData(0, -180.01, 30, 0)]
    [InlineData(0, 180.01, 30, 0)]
    [InlineData(0, 0, 4.99, 0)]
    [InlineData(0, 0, 200.01, 0)]
    [InlineData(0, 0, 30, -1)]
    [InlineData(0, 0, 30, 101)]
    public async Task CreatePoiAsync_GiaTriNgoaiKhoang_NemValidationException(double lat, double lng, double radius, int priority)
    {
        var request = new CreatePoiRequest
        {
            AreaId = Guid.NewGuid(),
            Lat = lat,
            Lng = lng,
            TriggerRadiusM = radius,
            Priority = priority
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreatePoiAsync(request));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Theory]
    [InlineData(-90, -180, 5, 0)]
    [InlineData(90, 180, 200, 100)]
    public async Task CreatePoiAsync_GiaTriBien_HopLe(double lat, double lng, double radius, int priority)
    {
        var areaId = Guid.NewGuid();
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TourArea { Id = areaId });
        _poiRepoMock.Setup(r => r.AddAsync(It.IsAny<Poi>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poi p, CancellationToken _) => p);

        var result = await _service.CreatePoiAsync(new CreatePoiRequest
        {
            AreaId = areaId,
            Lat = lat,
            Lng = lng,
            TriggerRadiusM = radius,
            Priority = priority
        });

        Assert.Equal(lat, result.Lat);
        Assert.Equal(radius, result.TriggerRadiusM);
    }

    [Fact]
    public async Task CreatePoiAsync_ImageUrlQua500KyTu_NemValidationException()
    {
        var request = ValidCreate(Guid.NewGuid());
        request.ImageUrl = new string('a', 501);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreatePoiAsync(request));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("image_url", ex.Message);
    }

    [Fact]
    public async Task GetPoiByIdAsync_KhongTonTai_NemNotFound()
    {
        var id = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Poi?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetPoiByIdAsync(id));

        Assert.Equal(ErrorCodes.NotFound, ex.Code);
    }

    [Fact]
    public async Task GetPoiByIdAsync_HopLe_TraVePoi()
    {
        var id = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Poi { Id = id, AreaId = Guid.NewGuid(), Priority = 5 });

        var result = await _service.GetPoiByIdAsync(id);

        Assert.Equal(id, result.Id);
        Assert.Equal(5, result.Priority);
    }

    [Fact]
    public async Task GetPoisAsync_PhanTrang_TraVeDanhSachVaTongSo()
    {
        var areaId = Guid.NewGuid();
        var items = new List<Poi>
        {
            new() { Id = Guid.NewGuid(), AreaId = areaId },
            new() { Id = Guid.NewGuid(), AreaId = areaId }
        };
        _poiRepoMock.Setup(r => r.GetPagedAsync(areaId, true, 2, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 5));

        var result = await _service.GetPoisAsync(areaId, true, 2, 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPoisAsync_PhanTrangSai_NemValidationException(int page, int pageSize)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetPoisAsync(null, null, page, pageSize));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task GetPoisAsync_PageSize100_HopLe()
    {
        _poiRepoMock.Setup(r => r.GetPagedAsync(null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Poi>(), 0));

        var result = await _service.GetPoisAsync(null, null, 1, 100);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task UpdatePoiAsync_KhongTonTai_NemNotFound()
    {
        var id = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Poi?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdatePoiAsync(id, new UpdatePoiRequest { TriggerRadiusM = 30 }));
    }

    [Fact]
    public async Task UpdatePoiAsync_RadiusSai_NemValidationException()
    {
        var id = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Poi { Id = id });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdatePoiAsync(id, new UpdatePoiRequest { TriggerRadiusM = 500 }));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        _poiRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Poi>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePoiAsync_HopLe_CapNhatVaDoiUpdatedAt()
    {
        var id = Guid.NewGuid();
        var oldTime = DateTime.UtcNow.AddDays(-1);
        var poi = new Poi { Id = id, AreaId = Guid.NewGuid(), Lat = 1, Lng = 1, TriggerRadiusM = 30, UpdatedAt = oldTime };
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(poi);

        var result = await _service.UpdatePoiAsync(id, new UpdatePoiRequest
        {
            Lat = 21.1,
            Lng = 105.1,
            TriggerRadiusM = 50,
            Priority = 20,
            ImageUrl = "https://example.com/b.jpg",
            IsActive = false
        });

        Assert.Equal(21.1, result.Lat);
        Assert.Equal(50, result.TriggerRadiusM);
        Assert.Equal(20, result.Priority);
        Assert.False(result.IsActive);
        Assert.True(result.UpdatedAt > oldTime);
        _poiRepoMock.Verify(r => r.UpdateAsync(poi, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePoiAsync_XoaMem_IsActiveFalse()
    {
        var id = Guid.NewGuid();
        var poi = new Poi { Id = id, IsActive = true };
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(poi);

        await _service.DeletePoiAsync(id);

        Assert.False(poi.IsActive);
        _poiRepoMock.Verify(r => r.UpdateAsync(It.Is<Poi>(p => !p.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePoiAsync_KhongTonTai_NemNotFound()
    {
        var id = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Poi?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeletePoiAsync(id));
    }
}
