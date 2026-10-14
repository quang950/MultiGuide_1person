using System.Security.Cryptography;
using System.Text;
using Moq;
using MultiGuide.Business.Common;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Payments;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Tests.Services;

public class PassServiceTests
{
    private readonly Mock<IAreaRepository> _areaRepoMock;
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IAccessPassRepository> _accessPassRepoMock;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly Mock<IClock> _clockMock;
    private readonly PassService _service;

    private readonly DateTime _fixedNow = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    public PassServiceTests()
    {
        _areaRepoMock = new Mock<IAreaRepository>();
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _accessPassRepoMock = new Mock<IAccessPassRepository>();
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _clockMock = new Mock<IClock>();

        _clockMock.Setup(c => c.UtcNow).Returns(_fixedNow);

        _service = new PassService(
            _areaRepoMock.Object,
            _paymentRepoMock.Object,
            _accessPassRepoMock.Object,
            _paymentGatewayMock.Object,
            _clockMock.Object);
    }

    [Fact]
    public async Task CreatePassAsync_RequestNull_NemArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreatePassAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreatePassAsync_AreaCodeRong_NemNotFoundException(string? areaCode)
    {
        var request = new CreatePassRequest
        {
            AreaCode = areaCode!,
            IdempotencyKey = "key-123"
        };

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreatePassAsync(request));
        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreatePassAsync_IdempotencyKeyRong_NemValidationException(string? key)
    {
        var request = new CreatePassRequest
        {
            AreaCode = "van-mieu",
            IdempotencyKey = key!
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreatePassAsync(request));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task CreatePassAsync_AreaKhongTonTai_NemNotFoundException()
    {
        // Arrange
        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("khu-la", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TourArea?)null);

        var request = new CreatePassRequest
        {
            AreaCode = "khu-la",
            IdempotencyKey = "key-123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreatePassAsync(request));
        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Fact]
    public async Task CreatePassAsync_AreaDaBiTat_NemNotFoundException()
    {
        // Arrange
        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = "khu-tat",
            IsActive = false
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("khu-tat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        var request = new CreatePassRequest
        {
            AreaCode = "khu-tat",
            IdempotencyKey = "key-123"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.CreatePassAsync(request));
        Assert.Equal(ErrorCodes.AreaNotFound, ex.Code);
    }

    [Fact]
    public async Task CreatePassAsync_IdempotencyKeyTrungLap_NemConflictException()
    {
        // Arrange
        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = "van-mieu",
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("van-mieu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = "key-da-dung",
            Status = PaymentStatus.Succeeded
        };

        _paymentRepoMock
            .Setup(r => r.GetByIdempotencyKeyAsync("key-da-dung", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        var request = new CreatePassRequest
        {
            AreaCode = "van-mieu",
            IdempotencyKey = "key-da-dung"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.CreatePassAsync(request));
        Assert.Equal(ErrorCodes.DuplicateRequest, ex.Code);
    }

    [Fact]
    public async Task CreatePassAsync_ThanhToanThatBai_LuuPaymentStatusFailedVaNemPaymentRequiredException()
    {
        // Arrange
        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = "van-mieu",
            PriceUsd = 5.0m,
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("van-mieu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        _paymentRepoMock
            .Setup(r => r.GetByIdempotencyKeyAsync("key-fail", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        _paymentGatewayMock
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentResult.Failed("mock", "Insufficient funds"));

        Payment? savedPayment = null;
        _paymentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => savedPayment = p)
            .ReturnsAsync((Payment p, CancellationToken _) => p);

        var request = new CreatePassRequest
        {
            AreaCode = "van-mieu",
            IdempotencyKey = "key-fail"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PaymentRequiredException>(() => _service.CreatePassAsync(request));
        Assert.Equal(ErrorCodes.PaymentFailed, ex.Code);
        Assert.Contains("Insufficient funds", ex.Message);

        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Failed, savedPayment.Status);
        Assert.Equal("mock", savedPayment.Provider);

        // Không được sinh AccessPass khi thanh toán thất bại
        _accessPassRepoMock.Verify(r => r.AddAsync(It.IsAny<AccessPass>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePassAsync_ThanhToanThanhCong_TaoPaymentVaPassChuanXac()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var area = new TourArea
        {
            Id = areaId,
            Code = "van-mieu",
            PriceUsd = 5.0m,
            PassTtlHours = 24,
            IsActive = true
        };

        _areaRepoMock
            .Setup(r => r.GetByCodeAsync("van-mieu", It.IsAny<CancellationToken>()))
            .ReturnsAsync(area);

        _paymentRepoMock
            .Setup(r => r.GetByIdempotencyKeyAsync("key-success", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        _paymentGatewayMock
            .Setup(g => g.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentResult.Success("mock", "mock_tx_99999"));

        Payment? savedPayment = null;
        _paymentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => savedPayment = p)
            .ReturnsAsync((Payment p, CancellationToken _) => p);

        AccessPass? savedPass = null;
        _accessPassRepoMock
            .Setup(r => r.AddAsync(It.IsAny<AccessPass>(), It.IsAny<CancellationToken>()))
            .Callback<AccessPass, CancellationToken>((pass, _) => savedPass = pass)
            .ReturnsAsync((AccessPass pass, CancellationToken _) => pass);

        var request = new CreatePassRequest
        {
            AreaCode = "van-mieu",
            IdempotencyKey = "key-success"
        };

        // Act
        var response = await _service.CreatePassAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        // Token 32 byte hex có 64 ký tự
        Assert.Equal(64, response.AccessToken.Length);
        Assert.Equal(_fixedNow.AddHours(24), response.ExpiresAt);
        Assert.Equal(areaId, response.AreaId);
        Assert.Equal("van-mieu", response.AreaCode);
        Assert.Equal(5.0m, response.PriceUsd);

        // Kiểm tra Payment đã lưu
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Succeeded, savedPayment.Status);
        Assert.Equal("mock", savedPayment.Provider);
        Assert.Equal("mock_tx_99999", savedPayment.ProviderRef);
        Assert.Equal(5.0m, savedPayment.Amount);

        // Kiểm tra AccessPass đã lưu
        Assert.NotNull(savedPass);
        Assert.Equal(areaId, savedPass.AreaId);
        Assert.Equal(savedPayment.Id, savedPass.PaymentId);
        Assert.Equal(AccessPassStatus.Active, savedPass.Status);
        Assert.Equal(_fixedNow, savedPass.IssuedAt);
        Assert.Equal(_fixedNow.AddHours(24), savedPass.ExpiresAt);

        // Kiểm tra TokenHash lưu trong DB khớp chuẩn SHA-256 của AccessToken
        var expectedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(response.AccessToken));
        var expectedHash = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();
        Assert.Equal(expectedHash, savedPass.TokenHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateTokenAsync_TokenRong_NemUnauthorizedException(string? token)
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _service.ValidateTokenAsync(token!));
        Assert.Equal(ErrorCodes.PassMissing, ex.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_KhongTimThayToken_NemForbiddenException_PassInvalid()
    {
        _accessPassRepoMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccessPass?)null);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _service.ValidateTokenAsync("invalid-token-123"));
        Assert.Equal(ErrorCodes.PassInvalid, ex.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenBiRevoked_NemForbiddenException_PassInvalid()
    {
        var revokedPass = new AccessPass
        {
            Id = Guid.NewGuid(),
            AreaId = Guid.NewGuid(),
            TokenHash = "somehash",
            ExpiresAt = _fixedNow.AddHours(5),
            Status = AccessPassStatus.Revoked
        };

        _accessPassRepoMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedPass);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _service.ValidateTokenAsync("revoked-token-123"));
        Assert.Equal(ErrorCodes.PassInvalid, ex.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenHetHan_NemForbiddenException_PassExpired()
    {
        var expiredPass = new AccessPass
        {
            Id = Guid.NewGuid(),
            AreaId = Guid.NewGuid(),
            TokenHash = "somehash",
            ExpiresAt = _fixedNow.AddMinutes(-1), // Đã hết hạn so với _fixedNow
            Status = AccessPassStatus.Active
        };

        _accessPassRepoMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredPass);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _service.ValidateTokenAsync("expired-token-123"));
        Assert.Equal(ErrorCodes.PassExpired, ex.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenHopLe_TraVePassContext()
    {
        var passId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var rawToken = "my-valid-token-123";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var validPass = new AccessPass
        {
            Id = passId,
            AreaId = areaId,
            TokenHash = tokenHash,
            ExpiresAt = _fixedNow.AddHours(12),
            Status = AccessPassStatus.Active
        };

        _accessPassRepoMock.Setup(r => r.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validPass);

        var context = await _service.ValidateTokenAsync(rawToken);

        Assert.NotNull(context);
        Assert.Equal(passId, context.PassId);
        Assert.Equal(areaId, context.AreaId);
        Assert.Equal(_fixedNow.AddHours(12), context.ExpiresAt);
    }
}
