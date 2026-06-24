namespace Stratix.Application.DTOs.Auth;

public record LoginRequest(string Username, string Password);
public record AuthUserProfile(long Id, string Name, string Email, string Role, string RoleCode, string Department, long EmployeeId);
public record LoginResponse(string Token, long ExpiresIn, AuthUserProfile User);
public record UpdateMyProfileRequest(string Name, string Email, string Department);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string Password);
public record MessageResponse(string Message);
