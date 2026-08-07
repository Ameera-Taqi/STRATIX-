using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly long _expirationSeconds;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly bool _validateIssuerAudience;

    public JwtTokenService(IConfiguration configuration, IHostEnvironment environment)
    {
        var secret = configuration["Stratix:Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Stratix:Jwt:Secret must be configured via environment, user-secrets, or .env — not left empty.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        // Access tokens are short-lived; refresh tokens cover longer sessions (default 15 minutes).
        var expirationMs = long.TryParse(configuration["Stratix:Jwt:ExpirationMs"], out var ms) ? ms : 900_000;
        _expirationSeconds = Math.Clamp(expirationMs / 1000, 60, 3600);

        _issuer = configuration["Stratix:Jwt:Issuer"] ?? "stratix";
        _audience = configuration["Stratix:Jwt:Audience"] ?? "stratix-api";
        _validateIssuerAudience = environment.IsProduction()
            || string.Equals(configuration["Stratix:Jwt:ValidateIssuerAudience"], "true", StringComparison.OrdinalIgnoreCase);
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("userId", user.Id.ToString()),
            new Claim("orgId", user.OrganizationId.ToString()),
            new Claim("email", user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("name", user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_expirationSeconds),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public long GetExpirationSeconds() => _expirationSeconds;

    public bool TryValidate(string token, out long userId, out UserRole role)
    {
        userId = 0;
        role = UserRole.EMPLOYEE;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = _validateIssuerAudience,
                ValidateAudience = _validateIssuerAudience,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
            var idClaim = principal.FindFirst("userId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = principal.FindFirst("role")?.Value ?? principal.FindFirst(ClaimTypes.Role)?.Value;
            if (idClaim == null || roleClaim == null) return false;
            userId = long.Parse(idClaim);
            role = Enum.Parse<UserRole>(roleClaim);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
