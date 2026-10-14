using Moq;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class AdminContentServiceTranslationTests
{
    private readonly Mock<IPoiRepository> _poiRepoMock = new();
    private readonly Mock<ITranslationRepository> _translationRepoMock = new();
    private readonly Mock<ILanguageRepository> _languageRepoMock = new();
    private readonly AdminContentService _service;

    public AdminContentServiceTranslationTests()
    {
        _service = new AdminContentService(
            new Mock<IAreaRepository>().Object,
            _poiRepoMock.Object,
            _translationRepoMock.Object,
            _languageRepoMock.Object);
    }

    private void SetupPoi(Guid poiId) =>
        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Poi { Id = poiId });

    private void SetupLanguage(string code, bool isActive = true) =>
        _languageRepoMock.Setup(r => r.GetByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Language { Code = code, Name = code, IsActive = isActive });

    private static CreateTranslationRequest ValidCreate(string lang = "vi") => new()
    {
        LanguageCode = lang,
        Title = "Khuê Văn Các",
        Description = "Gác Khuê Văn là biểu tượng của Hà Nội."
    };

    [Fact]
    public async Task CreateTranslationAsync_HopLe_TaoThanhCong()
    {
        var poiId = Guid.NewGuid();
        SetupPoi(poiId);
        SetupLanguage("vi");
        _translationRepoMock.Setup(r => r.ExistsAsync(poiId, "vi", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _translationRepoMock.Setup(r => r.AddAsync(It.IsAny<PoiTranslation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PoiTranslation t, CancellationToken _) => t);

        var result = await _service.CreateTranslationAsync(poiId, ValidCreate());

        Assert.Equal(poiId, result.PoiId);
        Assert.Equal("vi", result.LanguageCode);
        Assert.Equal("Khuê Văn Các", result.Title);
        Assert.Null(result.AudioPath);
        _translationRepoMock.Verify(r => r.AddAsync(It.Is<PoiTranslation>(t => t.PoiId == poiId && t.LanguageCode == "vi"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTranslationAsync_MaNgonNguVietHoa_ChuanHoaVeChuThuong()
    {
        var poiId = Guid.NewGuid();
        SetupPoi(poiId);
        SetupLanguage("en");
        _translationRepoMock.Setup(r => r.ExistsAsync(poiId, "en", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _translationRepoMock.Setup(r => r.AddAsync(It.IsAny<PoiTranslation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PoiTranslation t, CancellationToken _) => t);

        var result = await _service.CreateTranslationAsync(poiId, ValidCreate("  EN "));

        Assert.Equal("en", result.LanguageCode);
    }

    [Fact]
    public async Task CreateTranslationAsync_TrungPoiVaNgonNgu_NemConflict_TranslationExists()
    {
        var poiId = Guid.NewGuid();
        SetupPoi(poiId);
        SetupLanguage("vi");
        _translationRepoMock.Setup(r => r.ExistsAsync(poiId, "vi", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.CreateTranslationAsync(poiId, ValidCreate()));

        Assert.Equal(ErrorCodes.TranslationExists, ex.Code);
        _translationRepoMock.Verify(r => r.AddAsync(It.IsAny<PoiTranslation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTranslationAsync_PoiKhongTonTai_NemNotFound()
    {
        var poiId = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>())).ReturnsAsync((Poi?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateTranslationAsync(poiId, ValidCreate()));

        Assert.Equal(ErrorCodes.NotFound, ex.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateTranslationAsync_NgonNguKhongTonTaiHoacBiTat_NemValidation(bool languageExistsButInactive)
    {
        var poiId = Guid.NewGuid();
        SetupPoi(poiId);
        if (languageExistsButInactive)
        {
            SetupLanguage("fr", isActive: false);
        }
        else
        {
            _languageRepoMock.Setup(r => r.GetByCodeAsync("fr", It.IsAny<CancellationToken>())).ReturnsAsync((Language?)null);
        }

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTranslationAsync(poiId, ValidCreate("fr")));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Theory]
    [InlineData("", "mô tả")]
    [InlineData("   ", "mô tả")]
    [InlineData("tiêu đề", "")]
    [InlineData("tiêu đề", "   ")]
    public async Task CreateTranslationAsync_ThieuTitleHoacDescription_NemValidation(string title, string description)
    {
        var request = new CreateTranslationRequest { LanguageCode = "vi", Title = title, Description = description };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTranslationAsync(Guid.NewGuid(), request));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task CreateTranslationAsync_TitleQua200KyTu_NemValidation()
    {
        var request = ValidCreate();
        request.Title = new string('a', 201);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTranslationAsync(Guid.NewGuid(), request));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.Contains("200", ex.Message);
    }

    [Fact]
    public async Task CreateTranslationAsync_Title200KyTu_HopLe()
    {
        var poiId = Guid.NewGuid();
        SetupPoi(poiId);
        SetupLanguage("vi");
        _translationRepoMock.Setup(r => r.ExistsAsync(poiId, "vi", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _translationRepoMock.Setup(r => r.AddAsync(It.IsAny<PoiTranslation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PoiTranslation t, CancellationToken _) => t);

        var request = ValidCreate();
        request.Title = new string('a', 200);

        var result = await _service.CreateTranslationAsync(poiId, request);

        Assert.Equal(200, result.Title.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcdefghijk")]
    public async Task CreateTranslationAsync_MaNgonNguSai_NemValidation(string code)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateTranslationAsync(Guid.NewGuid(), ValidCreate(code)));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task GetTranslationsAsync_PoiTonTai_TraVeDanhSach()
    {
        var poiId = Guid.NewGuid();
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByPoiIdAsync(poiId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PoiTranslation>
            {
                new() { Id = Guid.NewGuid(), PoiId = poiId, LanguageCode = "en", Title = "A", Description = "d" },
                new() { Id = Guid.NewGuid(), PoiId = poiId, LanguageCode = "vi", Title = "B", Description = "d" }
            });

        var result = await _service.GetTranslationsAsync(poiId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTranslationsAsync_PoiKhongTonTai_NemNotFound()
    {
        var poiId = Guid.NewGuid();
        _poiRepoMock.Setup(r => r.GetByIdAsync(poiId, It.IsAny<CancellationToken>())).ReturnsAsync((Poi?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetTranslationsAsync(poiId));
    }

    [Fact]
    public async Task GetTranslationByIdAsync_ThuocPoiKhac_NemNotFound()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PoiTranslation { Id = id, PoiId = Guid.NewGuid() });

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetTranslationByIdAsync(poiId, id));

        Assert.Equal(ErrorCodes.NotFound, ex.Code);
    }

    [Fact]
    public async Task GetTranslationByIdAsync_HopLe_TraVeBanDich()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PoiTranslation { Id = id, PoiId = poiId, LanguageCode = "vi", Title = "T", Description = "D" });

        var result = await _service.GetTranslationByIdAsync(poiId, id);

        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task UpdateTranslationAsync_HopLe_CapNhatTitleDescriptionVaUpdatedAt()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var oldTime = DateTime.UtcNow.AddDays(-1);
        var translation = new PoiTranslation { Id = id, PoiId = poiId, LanguageCode = "vi", Title = "Cũ", Description = "Cũ", UpdatedAt = oldTime };
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(translation);

        var result = await _service.UpdateTranslationAsync(poiId, id, new UpdateTranslationRequest { Title = " Mới ", Description = "Nội dung mới" });

        Assert.Equal("Mới", result.Title);
        Assert.Equal("vi", result.LanguageCode);
        Assert.True(result.UpdatedAt > oldTime);
        _translationRepoMock.Verify(r => r.UpdateAsync(translation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTranslationAsync_TitleQua200_NemValidation()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new PoiTranslation { Id = id, PoiId = poiId });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateTranslationAsync(poiId, id, new UpdateTranslationRequest { Title = new string('a', 201), Description = "d" }));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        _translationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PoiTranslation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTranslationAsync_KhongTonTai_NemNotFound()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((PoiTranslation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateTranslationAsync(poiId, id, new UpdateTranslationRequest { Title = "t", Description = "d" }));
    }

    [Fact]
    public async Task DeleteTranslationAsync_HopLe_GoiRepositoryXoa()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var translation = new PoiTranslation { Id = id, PoiId = poiId };
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(translation);

        await _service.DeleteTranslationAsync(poiId, id);

        _translationRepoMock.Verify(r => r.DeleteAsync(translation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTranslationAsync_KhongTonTai_NemNotFound()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((PoiTranslation?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteTranslationAsync(poiId, id));
        _translationRepoMock.Verify(r => r.DeleteAsync(It.IsAny<PoiTranslation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
