using Moq;
using MultiGuide.Business.Common;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class PlayLogServiceTests
{
    private readonly Mock<IPlayLogRepository> _playLogRepoMock;
    private readonly Mock<IPoiRepository> _poiRepoMock;
    private readonly Mock<ILanguageRepository> _languageRepoMock;
    private readonly Mock<IClock> _clockMock;
    private readonly PlayLogService _service;

    private readonly DateTime _fixedNow = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    public PlayLogServiceTests()
    {
        _playLogRepoMock = new Mock<IPlayLogRepository>();
        _poiRepoMock = new Mock<IPoiRepository>();
        _languageRepoMock = new Mock<ILanguageRepository>();
        _clockMock = new Mock<IClock>();

        _clockMock.Setup(c => c.UtcNow).Returns(_fixedNow);

        // Mặc định ngôn ngữ vi, en, ja tồn tại và active
        _languageRepoMock.Setup(r => r.GetByCodeAsync(It.Is<string>(s => s == "vi" || s == "en" || s == "ja"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) => new Language { Code = code, Name = code, IsActive = true });

        _service = new PlayLogService(
            _playLogRepoMock.Object,
            _poiRepoMock.Object,
            _languageRepoMock.Object,
            _clockMock.Object);
    }

    [Fact]
    public async Task CreatePlayLogAsync_PoiIdRong_NemValidationException()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.Empty,
            LanguageCode = "vi",
            ListenedSec = 60,
            Completed = true
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("poi_id"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("v")] // < 2 ký tự
    [InlineData("vietnameselongtext")] // > 10 ký tự
    public async Task CreatePlayLogAsync_LanguageCodeKhongHopLe_NemValidationException(string lang)
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.NewGuid(),
            LanguageCode = lang,
            ListenedSec = 60,
            Completed = true
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("language_code"));
    }

    [Fact]
    public async Task CreatePlayLogAsync_NgonNguKhongTonTai_NemValidationException()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.NewGuid(),
            LanguageCode = "fr",
            ListenedSec = 60,
            Completed = true
        };

        _languageRepoMock.Setup(r => r.GetByCodeAsync("fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Language?)null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("không tồn tại", ex.Message);
    }

    [Fact]
    public async Task CreatePlayLogAsync_NgonNguInactive_NemValidationException()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.NewGuid(),
            LanguageCode = "de",
            ListenedSec = 60,
            Completed = true
        };

        _languageRepoMock.Setup(r => r.GetByCodeAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Language { Code = "de", Name = "German", IsActive = false });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("vô hiệu hóa", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21601)] // > 6 giờ (21600s)
    public async Task CreatePlayLogAsync_ListenedSecNgoaiPhamVi_NemValidationException(int listenedSec)
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.NewGuid(),
            LanguageCode = "vi",
            ListenedSec = listenedSec,
            Completed = false
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.NotNull(ex.Errors);
        Assert.True(ex.Errors.ContainsKey("listened_sec"));
    }

    [Fact]
    public async Task CreatePlayLogAsync_PoiKhongTonTai_NemNotFoundException()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var poiId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = poiId,
            LanguageCode = "vi",
            ListenedSec = 100,
            Completed = true
        };

        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poi?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.PoiContentNotFound, ex.Code);
    }

    [Fact]
    public async Task CreatePlayLogAsync_PoiInactive_NemNotFoundException()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var poiId = Guid.NewGuid();
        var req = new CreatePlayLogRequest
        {
            PoiId = poiId,
            LanguageCode = "vi",
            ListenedSec = 100,
            Completed = true
        };

        var poi = new Poi
        {
            Id = poiId,
            AreaId = areaId,
            IsActive = false
        };

        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreatePlayLogAsync(passId, areaId, req));

        Assert.Equal(ErrorCodes.PoiContentNotFound, ex.Code);
    }

    [Fact]
    public async Task CreatePlayLogAsync_PassAreaKhacVoiPoiArea_NemForbiddenException()
    {
        var passId = Guid.NewGuid();
        var passAreaId = Guid.NewGuid();
        var poiAreaId = Guid.NewGuid();
        var poiId = Guid.NewGuid();

        var req = new CreatePlayLogRequest
        {
            PoiId = poiId,
            LanguageCode = "vi",
            ListenedSec = 100,
            Completed = true
        };

        var poi = new Poi
        {
            Id = poiId,
            AreaId = poiAreaId, // khác passAreaId
            IsActive = true
        };

        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.CreatePlayLogAsync(passId, passAreaId, req));

        Assert.Equal(ErrorCodes.PassAreaMismatch, ex.Code);
    }

    [Fact]
    public async Task CreatePlayLogAsync_HopLe_LuuPlayLogVaTraVePlayLogResponse()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var poiId = Guid.NewGuid();

        var req = new CreatePlayLogRequest
        {
            PoiId = poiId,
            LanguageCode = "EN",
            ListenedSec = 150,
            Completed = true
        };

        var poi = new Poi
        {
            Id = poiId,
            AreaId = areaId,
            IsActive = true
        };

        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poi);

        _playLogRepoMock.Setup(r => r.AddAsync(It.IsAny<PlayLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayLog p, CancellationToken _) => p);

        var res = await _service.CreatePlayLogAsync(passId, areaId, req);

        Assert.NotNull(res);
        Assert.NotEqual(Guid.Empty, res.Id);
        Assert.Equal(passId, res.PassId);
        Assert.Equal(poiId, res.PoiId);
        Assert.Equal("en", res.LanguageCode);
        Assert.Equal(150, res.ListenedSec);
        Assert.True(res.Completed);
        Assert.Equal(_fixedNow, res.StartedAt);

        _playLogRepoMock.Verify(r => r.AddAsync(It.Is<PlayLog>(p =>
            p.PassId == passId &&
            p.PoiId == poiId &&
            p.LanguageCode == "en" &&
            p.ListenedSec == 150 &&
            p.Completed &&
            p.StartedAt == _fixedNow), It.IsAny<CancellationToken>()), Times.Once);
    }
}
