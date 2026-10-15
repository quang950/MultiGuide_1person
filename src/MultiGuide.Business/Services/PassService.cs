using System.Security.Cryptography;
using System.Text;
using MultiGuide.Business.Common;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Payments;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class PassService : IPassService
{
    private readonly IAreaRepository _areaRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IAccessPassRepository _accessPassRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IClock _clock;

    public PassService(
        IAreaRepository areaRepository,
        IPaymentRepository paymentRepository,
        IAccessPassRepository accessPassRepository,
        IPaymentGateway paymentGateway,
        IClock clock)
    {
        _areaRepository = areaRepository;
        _paymentRepository = paymentRepository;
        _accessPassRepository = accessPassRepository;
        _paymentGateway = paymentGateway;
        _clock = clock;
    }

    public async Task<CreatePassResponse> CreatePassAsync(CreatePassRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.AreaCode))
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, "Mã khu tham quan không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Idempotency-Key không được để trống.");
        }

        var normalizedAreaCode = request.AreaCode.Trim().ToLowerInvariant();
        var normalizedIdempotencyKey = request.IdempotencyKey.Trim();

        var area = await _areaRepository.GetByCodeAsync(normalizedAreaCode, cancellationToken);
        if (area == null || !area.IsActive)
        {
            throw new NotFoundException(
                ErrorCodes.AreaNotFound,
                $"Khu tham quan với mã '{request.AreaCode}' không tồn tại hoặc đã ngừng hoạt động.");
        }

        var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(normalizedIdempotencyKey, cancellationToken);
        if (existingPayment != null)
        {
            throw new ConflictException(
                ErrorCodes.DuplicateRequest,
                $"Giao dịch với Idempotency-Key '{normalizedIdempotencyKey}' đã tồn tại.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AreaId = area.Id,
            Amount = area.PriceUsd,
            Currency = "USD",
            Provider = "mock",
            IdempotencyKey = normalizedIdempotencyKey,
            Status = PaymentStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        var paymentRequest = new PaymentRequest
        {
            Amount = area.PriceUsd,
            Currency = "USD",
            IdempotencyKey = normalizedIdempotencyKey,
            AreaId = area.Id
        };

        var paymentResult = await _paymentGateway.ProcessPaymentAsync(paymentRequest, cancellationToken);

        if (!paymentResult.IsSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            payment.Provider = paymentResult.Provider;
            payment.ProviderRef = paymentResult.ProviderRef;
            await _paymentRepository.AddAsync(payment, cancellationToken);

            throw new PaymentRequiredException(
                ErrorCodes.PaymentFailed,
                paymentResult.ErrorMessage ?? "Giao dịch thanh toán bị từ chối.");
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.Provider = paymentResult.Provider;
        payment.ProviderRef = paymentResult.ProviderRef;
        await _paymentRepository.AddAsync(payment, cancellationToken);

        // Sinh 32-byte CSPRNG token và băm SHA-256
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var accessToken = Convert.ToHexString(randomBytes).ToLowerInvariant();

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken));
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var now = _clock.UtcNow;
        var expiresAt = now.AddHours(area.PassTtlHours);

        var pass = new AccessPass
        {
            Id = Guid.NewGuid(),
            AreaId = area.Id,
            PaymentId = payment.Id,
            TokenHash = tokenHash,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            Status = AccessPassStatus.Active
        };

        await _accessPassRepository.AddAsync(pass, cancellationToken);

        return new CreatePassResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            AreaId = area.Id,
            AreaCode = area.Code,
            PriceUsd = area.PriceUsd
        };
    }

    public async Task<PassContext> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedException(ErrorCodes.PassMissing, "Token truy cập không được để trống.");
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var pass = await _accessPassRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (pass == null || pass.Status == AccessPassStatus.Revoked)
        {
            throw new ForbiddenException(ErrorCodes.PassInvalid, "Token truy cập không hợp lệ hoặc đã bị thu hồi.");
        }

        if (pass.ExpiresAt <= _clock.UtcNow)
        {
            throw new ForbiddenException(ErrorCodes.PassExpired, "Token truy cập đã hết hạn.");
        }

        return new PassContext
        {
            PassId = pass.Id,
            AreaId = pass.AreaId,
            ExpiresAt = pass.ExpiresAt
        };
    }

    public async Task<CurrentPassResponse> GetCurrentPassAsync(Guid passId, CancellationToken cancellationToken = default)
    {
        var pass = await _accessPassRepository.GetByIdAsync(passId, cancellationToken);
        if (pass == null)
        {
            throw new NotFoundException(ErrorCodes.PassMissing, "Vé tham quan không tồn tại.");
        }

        var now = _clock.UtcNow;
        var remainingSeconds = (int)Math.Max(0, (pass.ExpiresAt - now).TotalSeconds);

        return new CurrentPassResponse
        {
            Id = pass.Id,
            AreaId = pass.AreaId,
            Status = pass.Status.ToString().ToLowerInvariant(),
            IssuedAt = pass.IssuedAt,
            ExpiresAt = pass.ExpiresAt,
            RemainingSeconds = remainingSeconds
        };
    }
}
