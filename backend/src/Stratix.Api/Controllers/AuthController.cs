using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IPasswordResetService _passwordReset;

    public AuthController(IAuthService auth, IPasswordResetService passwordReset)
    {
        _auth = auth;
        _passwordReset = passwordReset;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterOrganizationRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.RegisterOrganizationAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.LoginAsync(request.Username, request.Password, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(new { message = "Invalid credentials" }); }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.RefreshAsync(request.RefreshToken, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(new { message = "Invalid refresh token" }); }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
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
