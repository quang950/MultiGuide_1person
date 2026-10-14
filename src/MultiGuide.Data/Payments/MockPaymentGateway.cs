using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiGuide.Business.Payments;

namespace MultiGuide.Data.Payments;

/// <summary>
/// Cổng thanh toán giả lập (MockPaymentGateway) phục vụ kiểm thử và môi trường demo (US-02).
/// Cho phép cấu hình kết quả thanh toán thành công hoặc thất bại thông qua cấu hình "Payment:MockResult" hoặc "Payment__MockResult".
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    private const string DefaultProvider = "mock";
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockPaymentGateway> _logger;

    public MockPaymentGateway(
        IConfiguration configuration,
        ILogger<MockPaymentGateway> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var configuredResult = _configuration["Payment:MockResult"]
            ?? _configuration["Payment__MockResult"]
            ?? Environment.GetEnvironmentVariable("Payment__MockResult");

        if (string.Equals(configuredResult, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(configuredResult, "failure", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "MockPaymentGateway: Giả lập thanh toán THẤT BẠI cho IdempotencyKey '{IdempotencyKey}', số tiền: {Amount} {Currency}",
                request.IdempotencyKey,
                request.Amount,
                request.Currency);

            return Task.FromResult(PaymentResult.Failed(DefaultProvider, "Mock payment was declined by issuer."));
        }

        var providerRef = $"mock_tx_{Guid.NewGuid():N}";

        _logger.LogInformation(
            "MockPaymentGateway: Giả lập thanh toán THÀNH CÔNG mã giao dịch '{ProviderRef}', số tiền: {Amount} {Currency}",
            providerRef,
            request.Amount,
            request.Currency);

        return Task.FromResult(PaymentResult.Success(DefaultProvider, providerRef));
    }
}
