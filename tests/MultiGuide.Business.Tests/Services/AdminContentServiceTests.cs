using Moq;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Business.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class AdminContentServiceTests
{
    private readonly Mock<IAreaRepository> _areaRepoMock;
    private readonly AdminContentService _service;

    public AdminContentServiceTests()
    {
        _areaRepoMock = new Mock<IAreaRepository>();
        _service = new AdminContentService(
            _areaRepoMock.Object,
            new Mock<IPoiRepository>().Object,
            new Mock<ITranslationRepository>().Object,
            new Mock<ILanguageRepository>().Object,
            new Mock<IFileStorage>().Object);
    }

    [Fact]
    public async Task CreateAreaAsync_DuLieuHopLe_TaoThanhCong()
    {
        // Arrange
        var request = new CreateAreaRequest
        {
            Code = "van-mieu",
            Name = "Văn Miếu - Quốc Tử Giám",
            CenterLat = 21.0285,
            CenterLng = 105.8355,
            RadiusM = 300,
            PriceUsd = 5.0m,
            PassTtlHours = 24
        };

        _areaRepoMock
            .Setup(r => r.ExistsByCodeAsync("van-mieu", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _areaRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TourArea>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea a, CancellationToken _) => a);

        // Act
        var result = await _service.CreateAreaAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("van-mieu", result.Code);
        Assert.Equal("Văn Miếu - Quốc Tử Giám", result.Name);
        Assert.Equal(5.0m, result.PriceUsd);
        Assert.True(result.IsActive);
        _areaRepoMock.Verify(r => r.AddAsync(It.Is<TourArea>(a => a.Code == "van-mieu" && a.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAreaAsync_CodeRong_NemValidationException(string code)
    {
        var request = new CreateAreaRequest
        {
            Code = code,
            Name = "Văn Miếu",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = 5.0m
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAreaAsync(request));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Theory]
    [InlineData("Van-Mieu")]
    [InlineData("van_mieu")]
    [InlineData("van mieu")]
    [InlineData("van@mieu")]
    [InlineData("van.mieu")]
    public async Task CreateAreaAsync_CodeSaiDinhDang_NemValidationException(string code)
    {
        var request = new CreateAreaRequest
        {
            Code = code,
            Name = "Văn Miếu",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = 5.0m
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAreaAsync(request));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("chữ cái thường", ex.Message);
    }

    [Fact]
    public async Task CreateAreaAsync_CodeDaiQua50KyTu_NemValidationException()
    {
        var request = new CreateAreaRequest
        {
            Code = new string('a', 51),
            Name = "Văn Miếu",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = 5.0m
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAreaAsync(request));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("50", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(-0.01)]
    public async Task CreateAreaAsync_GiaNhoHonHoacBang0_NemValidationException(decimal price)
    {
        var request = new CreateAreaRequest
        {
            Code = "van-mieu",
            Name = "Văn Miếu",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = price
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAreaAsync(request));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("price_usd", ex.Message);
    }

    [Fact]
    public async Task CreateAreaAsync_MaKhuDaTonTai_NemConflictException_DuplicateRequest()
    {
        var request = new CreateAreaRequest
        {
            Code = "van-mieu",
            Name = "Văn Miếu",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = 5.0m
        };

        _areaRepoMock
            .Setup(r => r.ExistsByCodeAsync("van-mieu", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAreaAsync(request));
        Assert.Equal(ErrorCodes.DuplicateRequest, ex.Code);
    }

    [Fact]
    public async Task GetAreaByIdAsync_IdKhongTonTai_NemNotFoundException_AreaNotFound()
    {
        var id = Guid.NewGuid();
        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAreaByIdAsync(id));
        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Fact]
    public async Task GetAreaByIdAsync_IdHopLe_TraVeAreaResponse()
    {
        var id = Guid.NewGuid();
        var area = new TourArea
        {
            Id = id,
            Code = "van-mieu",
            Name = "Văn Miếu",
            PriceUsd = 10m,
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        var result = await _service.GetAreaByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("van-mieu", result.Code);
        Assert.Equal("Văn Miếu", result.Name);
    }

    [Fact]
    public async Task GetAreasAsync_GoiThanhCong_TraVeDanhSach()
    {
        var areas = new List<TourArea>
        {
            new() { Id = Guid.NewGuid(), Code = "van-mieu", Name = "Văn Miếu", PriceUsd = 5m, IsActive = true },
            new() { Id = Guid.NewGuid(), Code = "hoang-thanh", Name = "Hoàng Thành", PriceUsd = 8m, IsActive = true }
        };

        _areaRepoMock
            .Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(areas);

        var result = await _service.GetAreasAsync(true);

        Assert.Equal(2, result.Count);
        Assert.Equal("van-mieu", result[0].Code);
        Assert.Equal("hoang-thanh", result[1].Code);
    }

    [Fact]
    public async Task UpdateAreaAsync_IdKhongTonTai_NemNotFoundException()
    {
        var id = Guid.NewGuid();
        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea?)null);

        var request = new UpdateAreaRequest
        {
            Name = "Văn Miếu Updated",
            PriceUsd = 10m,
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 200,
            PassTtlHours = 24
        };

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAreaAsync(id, request));
        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Fact]
    public async Task UpdateAreaAsync_GiaKhongHopLe_NemValidationException()
    {
        var id = Guid.NewGuid();
        var existing = new TourArea { Id = id, Code = "van-mieu", Name = "Văn Miếu", PriceUsd = 5m };
        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UpdateAreaRequest
        {
            Name = "Văn Miếu Updated",
            PriceUsd = -1m,
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 200,
            PassTtlHours = 24
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAreaAsync(id, request));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task UpdateAreaAsync_HopLe_CapNhatThanhCong()
    {
        var id = Guid.NewGuid();
        var existing = new TourArea
        {
            Id = id,
            Code = "van-mieu",
            Name = "Văn Miếu",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = 5m,
            PassTtlHours = 24,
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UpdateAreaRequest
        {
            Name = "Văn Miếu Mới",
            CenterLat = 21.02,
            CenterLng = 105.83,
            RadiusM = 250,
            PriceUsd = 7.5m,
            PassTtlHours = 48,
            IsActive = true
        };

        var result = await _service.UpdateAreaAsync(id, request);

        Assert.Equal("Văn Miếu Mới", result.Name);
        Assert.Equal(7.5m, result.PriceUsd);
        Assert.Equal(48, result.PassTtlHours);
        _areaRepoMock.Verify(r => r.UpdateAsync(It.Is<TourArea>(a => a.Name == "Văn Miếu Mới" && a.PriceUsd == 7.5m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAreaAsync_IdKhongTonTai_NemNotFoundException()
    {
        var id = Guid.NewGuid();
        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAreaAsync(id));
        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Fact]
    public async Task DeleteAreaAsync_IdHopLe_XoaMemThanhCong()
    {
        var id = Guid.NewGuid();
        var existing = new TourArea
        {
            Id = id,
            Code = "van-mieu",
            Name = "Văn Miếu",
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _service.DeleteAreaAsync(id);

        Assert.False(existing.IsActive);
        _areaRepoMock.Verify(r => r.UpdateAsync(It.Is<TourArea>(a => !a.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }
}
