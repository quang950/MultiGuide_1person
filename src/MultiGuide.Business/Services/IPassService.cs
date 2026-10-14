using MultiGuide.Business.DTOs.Client;

namespace MultiGuide.Business.Services;

/// <summary>
/// Quản lý cấp phát, xác thực vé AccessPass và giao dịch thanh toán (US-02, US-03, US-04).
/// </summary>
public interface IPassService
{
    /// <summary>
    /// Mua và cấp mới AccessPass cho khách tham quan với tính idempotent (US-02).
    /// </summary>
    Task<CreatePassResponse> CreatePassAsync(CreatePassRequest request, CancellationToken cancellationToken = default);
}
