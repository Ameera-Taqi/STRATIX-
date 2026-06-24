using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IPasswordResetService _passwordReset;

    public AuthController(IAuthService auth, IPasswordResetService passwordReset)
    {
        _auth = auth;
        _passwordReset = passwordReset;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.LoginAsync(request.Username, request.Password, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(new { message = "Invalid credentials" }); }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<AuthUserProfile> Me(CancellationToken ct) => await _auth.GetCurrentUserAsync(ct);

    [HttpPatch("me")]
    [Authorize]
    public async Task<AuthUserProfile> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken ct) =>
        await _auth.UpdateMyProfileAsync(request, ct);

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<MessageResponse> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct) =>
        await _passwordReset.ForgotPasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<MessageResponse> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct) =>
        await _passwordReset.ResetPasswordAsync(request, ct);
}
