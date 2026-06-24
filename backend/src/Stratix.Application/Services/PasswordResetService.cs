using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;

namespace Stratix.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetMailService _mail;
    private readonly string _tokenPepper;
    private readonly string _frontendUrl;
    private readonly int _maxPerHour;

    public PasswordResetService(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IPasswordResetMailService mail,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _mail = mail;
        _tokenPepper = configuration["Stratix:PasswordReset:TokenPepper"] ?? "stratix-reset-pepper";
        _frontendUrl = configuration["Stratix:FrontendUrl"] ?? "http://localhost:4200";
        _maxPerHour = int.TryParse(configuration["Stratix:PasswordReset:MaxPerHour"], out var m) ? m : 3;
    }

    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string? ip, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null)
            return new MessageResponse("If an account exists, a reset link has been sent.");

        var since = DateTimeOffset.UtcNow.AddHours(-1);
        var recent = await _db.PasswordResetTokens.CountAsync(t =>
            (t.RequestIp == ip || t.UserId == user.Id) && t.CreatedAt >= since, ct);
        if (recent >= _maxPerHour)
            return new MessageResponse("If an account exists, a reset link has been sent.");

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = HashToken(rawToken);
        _db.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RequestIp = ip,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        var link = $"{_frontendUrl.TrimEnd('/')}/auth/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await _mail.SendResetEmailAsync(user.Email, link, ct);
        return new MessageResponse("If an account exists, a reset link has been sent.");
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters");

        var hash = HashToken(request.Token);
        var token = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow, ct)
            ?? throw new ArgumentException("Invalid or expired reset token");

        token.User.Password = _passwordHasher.Hash(request.Password);
        token.User.UpdatedAt = DateTimeOffset.UtcNow;
        token.UsedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new MessageResponse("Password has been reset successfully.");
    }

    private string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_tokenPepper + token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
