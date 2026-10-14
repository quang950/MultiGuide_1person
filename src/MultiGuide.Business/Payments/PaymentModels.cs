namespace MultiGuide.Business.Payments;

/// <summary>
/// Yêu cầu xử lý thanh toán gửi đến cổng thanh toán (IPaymentGateway).
/// </summary>
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid AreaId { get; set; }
}

/// <summary>
/// Kết quả phản hồi từ cổng thanh toán (IPaymentGateway).
/// </summary>
public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderRef { get; set; }
    public string? ErrorMessage { get; set; }

    public static PaymentResult Success(string provider, string providerRef) => new()
    {
        IsSuccess = true,
        Provider = provider,
        ProviderRef = providerRef
    };

    public static PaymentResult Failed(string provider, string errorMessage) => new()
    {
        IsSuccess = false,
        Provider = provider,
        ErrorMessage = errorMessage
    };
}
