namespace MultiGuide.Business.DTOs.Admin;

public record AdminLoginRequest(string Email, string Password);

public record AdminLoginResponse(string AccessToken, string Email, string Role);
