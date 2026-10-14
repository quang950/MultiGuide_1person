using Moq;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Business.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class AdminContentServiceTranslationTests
{
    private readonly Mock<IPoiRepository> _poiRepoMock = new();
    private readonly Mock<ITranslationRepository> _translationRepoMock = new();
    private readonly Mock<ILanguageRepository> _languageRepoMock = new();
    private readonly Mock<IFileStorage> _fileStorageMock = new();
    private readonly AdminContentService _service;

    public AdminContentServiceTranslationTests()
    {
        _service = new AdminContentService(
            new Mock<IAreaRepository>().Object,
            _poiRepoMock.Object,
            _translationRepoMock.Object,
            _languageRepoMock.Object,
            _fileStorageMock.Object);
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

    [Fact]
    public async Task DeleteTranslationAsync_CoAudio_XoaFileKhoiStorageVaXoaDB()
    {
        var poiId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var translation = new PoiTranslation
        {
            Id = id,
            PoiId = poiId,
            LanguageCode = "vi",
            AudioPath = "pois/p1/vi.mp3"
        };
        SetupPoi(poiId);
        _translationRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(translation);

        await _service.DeleteTranslationAsync(poiId, id);

        _fileStorageMock.Verify(s => s.DeleteAsync("pois/p1/vi.mp3", It.IsAny<CancellationToken>()), Times.Once);
        _translationRepoMock.Verify(r => r.DeleteAsync(translation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_HopLe_Id3Tag_UploadThanhCong()
    {
        var translationId = Guid.NewGuid();
        var poiId = Guid.NewGuid();
        var translation = new PoiTranslation
        {
            Id = translationId,
            PoiId = poiId,
            LanguageCode = "vi",
            Title = "Khuê Văn Các",
            AudioPath = null
        };
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync(translation);
        _fileStorageMock.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string p, string _, CancellationToken _) => p);

        // ID3 tag header
        var mp3Bytes = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(mp3Bytes);

        var result = await _service.UploadTranslationAudioAsync(translationId, stream, mp3Bytes.Length, "audio/mpeg");

        Assert.Equal(translationId, result.Id);
        Assert.Equal($"pois/{poiId}/vi.mp3", result.AudioPath);
        _fileStorageMock.Verify(s => s.UploadAsync(stream, $"pois/{poiId}/vi.mp3", "audio/mpeg", It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _translationRepoMock.Verify(r => r.UpdateAsync(translation, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_HopLe_FrameSync_UploadThanhCong()
    {
        var translationId = Guid.NewGuid();
        var poiId = Guid.NewGuid();
        var translation = new PoiTranslation
        {
            Id = translationId,
            PoiId = poiId,
            LanguageCode = "en",
            Title = "Khue Van Pavilion"
        };
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync(translation);
        _fileStorageMock.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string p, string _, CancellationToken _) => p);

        // MPEG-1 Layer 3 Frame Sync header: 0xFF 0xFB
        var mp3Bytes = new byte[] { 0xFF, 0xFB, 0x90, 0x44, 0x00, 0x00 };
        using var stream = new MemoryStream(mp3Bytes);

        var result = await _service.UploadTranslationAudioAsync(translationId, stream, mp3Bytes.Length, "audio/mpeg");

        Assert.Equal(translationId, result.Id);
        Assert.Equal($"pois/{poiId}/en.mp3", result.AudioPath);
        _fileStorageMock.Verify(s => s.UploadAsync(stream, $"pois/{poiId}/en.mp3", "audio/mpeg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_DaCoAudioCu_XoaAudioCuVaCapNhatMoi()
    {
        var translationId = Guid.NewGuid();
        var poiId = Guid.NewGuid();
        var oldAudioPath = "pois/old/old_file.mp3";
        var translation = new PoiTranslation
        {
            Id = translationId,
            PoiId = poiId,
            LanguageCode = "vi",
            AudioPath = oldAudioPath
        };
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync(translation);
        _fileStorageMock.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string p, string _, CancellationToken _) => p);

        var mp3Bytes = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00 };
        using var stream = new MemoryStream(mp3Bytes);

        var result = await _service.UploadTranslationAudioAsync(translationId, stream, mp3Bytes.Length, "audio/mpeg");

        _fileStorageMock.Verify(s => s.DeleteAsync(oldAudioPath, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageMock.Verify(s => s.UploadAsync(stream, $"pois/{poiId}/vi.mp3", "audio/mpeg", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal($"pois/{poiId}/vi.mp3", result.AudioPath);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_SaiMagicBytes_NemValidationException()
    {
        var translationId = Guid.NewGuid();
        var translation = new PoiTranslation { Id = translationId, PoiId = Guid.NewGuid(), LanguageCode = "vi" };
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync(translation);

        // Header giả mạo (ví dụ file văn bản "Hello" hoặc file WAV "RIFF")
        var fakeBytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00 };
        using var stream = new MemoryStream(fakeBytes);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UploadTranslationAudioAsync(translationId, stream, fakeBytes.Length, "audio/mpeg"));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        _fileStorageMock.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_FileQua10MB_NemValidationException()
    {
        var translationId = Guid.NewGuid();
        var translation = new PoiTranslation { Id = translationId, PoiId = Guid.NewGuid(), LanguageCode = "vi" };
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync(translation);

        var mp3Bytes = new byte[] { 0x49, 0x44, 0x33, 0x03 };
        using var stream = new MemoryStream(mp3Bytes);
        long over10Mb = (10 * 1024 * 1024) + 1;

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UploadTranslationAudioAsync(translationId, stream, over10Mb, "audio/mpeg"));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_FileRong_NemValidationException()
    {
        var translationId = Guid.NewGuid();
        var translation = new PoiTranslation { Id = translationId, PoiId = Guid.NewGuid(), LanguageCode = "vi" };
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync(translation);

        using var stream = new MemoryStream();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UploadTranslationAudioAsync(translationId, stream, 0, "audio/mpeg"));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task UploadTranslationAudioAsync_BaoDichKhongTonTai_NemNotFound()
    {
        var translationId = Guid.NewGuid();
        _translationRepoMock.Setup(r => r.GetByIdAsync(translationId, It.IsAny<CancellationToken>())).ReturnsAsync((PoiTranslation?)null);

        var mp3Bytes = new byte[] { 0x49, 0x44, 0x33, 0x03 };
        using var stream = new MemoryStream(mp3Bytes);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UploadTranslationAudioAsync(translationId, stream, mp3Bytes.Length, "audio/mpeg"));

        Assert.Equal(ErrorCodes.NotFound, ex.Code);
    }
}
