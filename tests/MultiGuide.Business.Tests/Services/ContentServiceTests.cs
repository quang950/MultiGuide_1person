using Moq;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Business.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class ContentServiceTests
{
    private readonly Mock<IPoiRepository> _poiRepoMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly ContentService _service;

    private readonly Guid _validAreaId = Guid.NewGuid();
    private readonly Guid _poiId = Guid.NewGuid();

    public ContentServiceTests()
    {
        _poiRepoMock = new Mock<IPoiRepository>();
        _fileStorageMock = new Mock<IFileStorage>();
        _service = new ContentService(_poiRepoMock.Object, _fileStorageMock.Object);
    }

    [Fact]
    public async Task GetPoiDetailAsync_PoiKhongTonTai_NemNotFoundException_PoiContentNotFound()
    {
        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poi?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi"));

        Assert.Equal(ErrorCodes.PoiContentNotFound, ex.Code);
    }

    [Fact]
    public async Task GetPoiDetailAsync_PoiIsActiveFalse_NemNotFoundException_PoiContentNotFound()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = false
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi"));

        Assert.Equal(ErrorCodes.PoiContentNotFound, ex.Code);
    }

    [Fact]
    public async Task GetPoiDetailAsync_PoiSaiKhuVucCuaPass_NemForbiddenException_PassAreaMismatch()
    {
        var otherAreaId = Guid.NewGuid();
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = otherAreaId, // Khác passAreaId
            IsActive = true
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi"));

        Assert.Equal(ErrorCodes.PassAreaMismatch, ex.Code);
    }

    [Fact]
    public async Task GetPoiDetailAsync_PoiKhongCoBanDich_NemNotFoundException_PoiContentNotFound()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations = []
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi"));

        Assert.Equal(ErrorCodes.PoiContentNotFound, ex.Code);
    }

    [Fact]
    public async Task GetPoiDetailAsync_DungNgonNguYeuCau_TraVeDungBanDichVaFallbackNull()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Cổng Văn Miếu", Description = "Mô tả tiếng Việt" },
                new PoiTranslation { LanguageCode = "en", Title = "Van Mieu Gate", Description = "English description" }
            ]
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var res = await _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi");

        Assert.NotNull(res);
        Assert.Equal("Cổng Văn Miếu", res.Title);
        Assert.Equal("vi", res.Language);
        Assert.Null(res.FallbackLanguage);
    }

    [Fact]
    public async Task GetPoiDetailAsync_NgonNguKhongCo_FallbackSangEn()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Cổng Văn Miếu", Description = "Mô tả tiếng Việt" },
                new PoiTranslation { LanguageCode = "en", Title = "Van Mieu Gate", Description = "English description" }
            ]
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        // Yêu cầu tiếng Pháp (không có) -> fallback sang en
        var res = await _service.GetPoiDetailAsync(_poiId, _validAreaId, "fr");

        Assert.NotNull(res);
        Assert.Equal("Van Mieu Gate", res.Title);
        Assert.Equal("en", res.Language);
        Assert.Equal("en", res.FallbackLanguage);
    }

    [Fact]
    public async Task GetPoiDetailAsync_NgonNguKhongCoVaEnKhongCo_FallbackSangVi()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Cổng Văn Miếu", Description = "Mô tả tiếng Việt" }
            ]
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        // Yêu cầu tiếng Pháp (không có, en cũng không có) -> fallback sang vi
        var res = await _service.GetPoiDetailAsync(_poiId, _validAreaId, "fr");

        Assert.NotNull(res);
        Assert.Equal("Cổng Văn Miếu", res.Title);
        Assert.Equal("vi", res.Language);
        Assert.Equal("vi", res.FallbackLanguage);
    }

    [Fact]
    public async Task GetPoiDetailAsync_KhongTruyenNgonNgu_LayEnVaFallbackNull()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Cổng Văn Miếu", Description = "Mô tả tiếng Việt" },
                new PoiTranslation { LanguageCode = "en", Title = "Van Mieu Gate", Description = "English description" }
            ]
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var res = await _service.GetPoiDetailAsync(_poiId, _validAreaId, null);

        Assert.NotNull(res);
        Assert.Equal("Van Mieu Gate", res.Title);
        Assert.Equal("en", res.Language);
        Assert.Null(res.FallbackLanguage);
    }

    [Fact]
    public async Task GetPoiDetailAsync_CoAudioPath_GoiCreateSignedUrl1Gio()
    {
        var audioPath = "pois/audio/gate.mp3";
        var signedUrl = "https://supabase.co/storage/v1/object/sign/audio/gate.mp3?token=xyz";

        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations =
            [
                new PoiTranslation
                {
                    LanguageCode = "vi",
                    Title = "Cổng Văn Miếu",
                    AudioPath = audioPath,
                    AudioDurationSec = 120
                }
            ]
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        _fileStorageMock.Setup(s => s.CreateSignedUrlAsync(audioPath, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signedUrl);

        var res = await _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi");

        Assert.NotNull(res);
        Assert.Equal(signedUrl, res.AudioUrl);
        Assert.Equal(120, res.AudioDurationSec);
        _fileStorageMock.Verify(s => s.CreateSignedUrlAsync(audioPath, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPoiDetailAsync_KhongCoAudioPath_TraVeAudioUrlNull()
    {
        var poi = new Poi
        {
            Id = _poiId,
            AreaId = _validAreaId,
            IsActive = true,
            Translations =
            [
                new PoiTranslation
                {
                    LanguageCode = "vi",
                    Title = "Cổng Văn Miếu",
                    AudioPath = null
                }
            ]
        };

        _poiRepoMock.Setup(r => r.GetWithTranslationsByIdAsync(_poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var res = await _service.GetPoiDetailAsync(_poiId, _validAreaId, "vi");

        Assert.NotNull(res);
        Assert.Null(res.AudioUrl);
        _fileStorageMock.Verify(s => s.CreateSignedUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
