namespace MultiGuide.Api.Auth;

/// <summary>
/// Đánh dấu endpoint yêu cầu xác thực bằng AccessPass token (US-03).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireAccessPassAttribute : Attribute
{
}
