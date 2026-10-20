using MultiGuide.Business.DTOs.Admin;

namespace MultiGuide.Business.Services;

public interface IAdminAuthService
{
    Task<AdminLoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
