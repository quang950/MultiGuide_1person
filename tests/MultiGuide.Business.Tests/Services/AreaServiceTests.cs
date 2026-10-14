using Moq;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class AreaServiceTests
{
    private readonly Mock<IAreaRepository> _areaRepoMock;
    private readonly AreaService _service;

    public AreaServiceTests()
    {
        _areaRepoMock = new Mock<IAreaRepository>();
        _service = new AreaService(_areaRepoMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetAreaByCodeAsync_CodeRongHoacNull_NemNotFoundException(string? code)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetAreaByCodeAsync(code!));

        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Fact]
    public async Task GetAreaByCodeAsync_KhongTimThayKhu_NemNotFoundException()
    {
        // Arrange
        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("khu-khong-ton-tai", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetAreaByCodeAsync("khu-khong-ton-tai"));

        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
        Assert.Contains("khu-khong-ton-tai", ex.Message);
    }

    [Fact]
    public async Task GetAreaByCodeAsync_KhuDaBiTatIsActiveFalse_NemNotFoundException()
    {
        // Arrange
        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = "khu-tat",
            Name = "Khu đã tắt",
            IsActive = false
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("khu-tat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetAreaByCodeAsync("khu-tat"));

        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
        Assert.Contains("khu-tat", ex.Message);
    }

    [Fact]
    public async Task GetAreaByCodeAsync_KhuHopLe_CoNgonNguHoTro_TraVeAreaDetailResponse()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var area = new TourArea
        {
            Id = areaId,
            Code = "van-mieu",
            Name = "Văn Miếu - Quốc Tử Giám",
            CenterLat = 21.0285,
            CenterLng = 105.8355,
            RadiusM = 300,
            PriceUsd = 5.0m,
            PassTtlHours = 24,
            IsActive = true
        };

        var languages = new List<Language>
        {
            new() { Code = "en", Name = "English", IsActive = true },
            new() { Code = "ja", Name = "Japanese", IsActive = true },
            new() { Code = "vi", Name = "Tiếng Việt", IsActive = true }
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("van-mieu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        _areaRepoMock
            .Setup(r => r.GetSupportedLanguagesAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(languages);

        // Act
        var result = await _service.GetAreaByCodeAsync("van-mieu");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(areaId, result.Id);
        Assert.Equal("van-mieu", result.Code);
        Assert.Equal("Văn Miếu - Quốc Tử Giám", result.Name);
        Assert.Equal(21.0285, result.CenterLat);
        Assert.Equal(105.8355, result.CenterLng);
        Assert.Equal(300, result.RadiusM);
        Assert.Equal(5.0m, result.PriceUsd);
        Assert.Equal(24, result.PassTtlHours);
        Assert.Equal(3, result.SupportedLanguages.Count);
        Assert.Equal("en", result.SupportedLanguages[0].Code);
        Assert.Equal("ja", result.SupportedLanguages[1].Code);
        Assert.Equal("vi", result.SupportedLanguages[2].Code);
    }

    [Fact]
    public async Task GetAreaByCodeAsync_KhuHopLe_KhongCoNgonNguNao_TraVeDanhSachRong()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var area = new TourArea
        {
            Id = areaId,
            Code = "khu-moi",
            Name = "Khu mới chưa có bản dịch",
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("khu-moi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        _areaRepoMock
            .Setup(r => r.GetSupportedLanguagesAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Language>());

        // Act
        var result = await _service.GetAreaByCodeAsync("khu-moi");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.SupportedLanguages);
    }

    [Fact]
    public async Task GetAreaByCodeAsync_CodeCoKhoangTrangVaChuHoa_ChuanHoaVaTimThanhCong()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var area = new TourArea
        {
            Id = areaId,
            Code = "van-mieu",
            Name = "Văn Miếu",
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("van-mieu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        _areaRepoMock
            .Setup(r => r.GetSupportedLanguagesAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Language>());

        // Act
        var result = await _service.GetAreaByCodeAsync("  VAN-MIEU  ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("van-mieu", result.Code);
        _areaRepoMock.Verify(r => r.GetByCodeAsync("van-mieu", It.IsAny<CancellationToken>()), Times.Once);
    }
}
