namespace MultiGuide.Business.Payments;

/// <summary>
/// Cổng xử lý thanh toán cho hệ thống mua AccessPass (US-02).
/// Tuân thủ nguyên lý Dependency Inversion: Business định nghĩa interface, Data hiện thực.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Xử lý thanh toán cho đơn mua AccessPass.
    /// </summary>
    /// <param name="request">Thông tin yêu cầu thanh toán</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Kết quả xử lý thanh toán (thành công hoặc thất bại)</returns>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}
