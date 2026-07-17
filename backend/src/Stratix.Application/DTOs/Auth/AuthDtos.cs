namespace Stratix.Application.DTOs.Auth;

public record LoginRequest(string Username, string Password);
public record RegisterOrganizationRequest(string OrganizationName, string AdminName, string AdminEmail, string Password, string? Slug = null);
public record AuthUserProfile(long Id, string Name, string Email, string Role, string RoleCode, string Department, long EmployeeId);
public record LoginResponse(string Token, long ExpiresIn, AuthUserProfile User, string? RefreshToken = null);
public record RefreshTokenRequest(string RefreshToken);
public record UpdateMyProfileRequest(string Name, string Email, string Department);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password);
public record MessageResponse(string Message);
