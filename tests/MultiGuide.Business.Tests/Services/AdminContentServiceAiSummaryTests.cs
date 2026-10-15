using Moq;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Business.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class AdminContentServiceAiSummaryTests
{
    private readonly Mock<IAreaRepository> _areaRepoMock = new();
    private readonly Mock<IPoiRepository> _poiRepoMock = new();
    private readonly Mock<ITranslationRepository> _translationRepoMock = new();
    private readonly Mock<ILanguageRepository> _languageRepoMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly Mock<IQrCodeGenerator> _qrCodeMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();

    private AdminContentService CreateService(bool withAi = true)
    {
        return new AdminContentService(
            _areaRepoMock.Object,
            _poiRepoMock.Object,
            _translationRepoMock.Object,
            _languageRepoMock.Object,
            _fileStorageMock.Object,
            _qrCodeMock.Object,
            withAi ? _aiServiceMock.Object : null);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_HopLe_GoiAiVaTraVeThanhCong()
    {
        var poiId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var area = new TourArea { Id = areaId, Code = "van-mieu", Name = "Văn Miếu - Quốc Tử Giám" };
        var poi = new Poi
        {
            Id = poiId,
            AreaId = areaId,
            Translations = new List<PoiTranslation>
            {
                new() { PoiId = poiId, LanguageCode = "vi", Title = "Khuê Văn Các" },
                new() { PoiId = poiId, LanguageCode = "en", Title = "Khue Van Pavilion" }
            }
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);
        _aiServiceMock.Setup(a => a.GeneratePoiSummaryAsync("Khuê Văn Các", "Văn Miếu - Quốc Tử Giám", "vi", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Khuê Văn Các là công trình biểu tượng của thủ đô Hà Nội...");

        var service = CreateService();
        var result = await service.GeneratePoiSummaryAsync(poiId, "vi");

        Assert.NotNull(result);
        Assert.Equal(poiId, result.PoiId);
        Assert.Equal("Khuê Văn Các", result.PoiName);
        Assert.Equal("Văn Miếu - Quốc Tử Giám", result.AreaName);
        Assert.Equal("vi", result.Language);
        Assert.Contains("Khuê Văn Các là công trình biểu tượng", result.Summary);
        Assert.Equal("gemini-2.5-flash", result.Model);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_KhongTruyenLanguage_MacDinhTiengViet()
    {
        var poiId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var poi = new Poi
        {
            Id = poiId,
            AreaId = areaId,
            Translations = new List<PoiTranslation>
            {
                new() { PoiId = poiId, LanguageCode = "vi", Title = "Cổng Văn Miếu" }
            }
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TourArea { Id = areaId, Name = "Văn Miếu" });
        _aiServiceMock.Setup(a => a.GeneratePoiSummaryAsync("Cổng Văn Miếu", "Văn Miếu", "vi", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Cổng Văn Miếu là cổng chính dẫn vào di tích...");

        var service = CreateService();
        var result = await service.GeneratePoiSummaryAsync(poiId, null);

        Assert.Equal("vi", result.Language);
        _aiServiceMock.Verify(a => a.GeneratePoiSummaryAsync("Cổng Văn Miếu", "Văn Miếu", "vi", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_PoiKhongTonTai_NemNotFoundException()
    {
        var poiId = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poi?)null);

        var service = CreateService();
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GeneratePoiSummaryAsync(poiId, "vi"));

        Assert.Equal(ErrorCodes.NotFound, ex.Code);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_ChuaDangKyAiService_NemInvalidOperationException()
    {
        var poiId = Guid.NewGuid();
        var service = CreateService(withAi: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GeneratePoiSummaryAsync(poiId, "vi"));
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_PoiChuaCoTranslation_DungTenMacDinh()
    {
        var poiId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var poi = new Poi
        {
            Id = poiId,
            AreaId = areaId,
            Translations = new List<PoiTranslation>()
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);
        _areaRepoMock.Setup(r => r.GetByIdAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TourArea { Id = areaId, Name = "Khu bảo tồn" });
        _aiServiceMock.Setup(a => a.GeneratePoiSummaryAsync(It.IsAny<string>(), "Khu bảo tồn", "vi", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tóm tắt điểm tham quan mới...");

        var service = CreateService();
        var result = await service.GeneratePoiSummaryAsync(poiId, "vi");

        Assert.NotNull(result);
        Assert.Contains("Điểm tham quan", result.PoiName);
    }
}
