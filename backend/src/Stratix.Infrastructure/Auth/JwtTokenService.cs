using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly long _expirationSeconds;

    public JwtTokenService(IConfiguration configuration)
    {
        var secret = configuration["Stratix:Jwt:Secret"] ?? "change-this-secret-in-production-min-256-bits-long";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var expirationMs = long.TryParse(configuration["Stratix:Jwt:ExpirationMs"], out var ms) ? ms : 86_400_000;
        _expirationSeconds = expirationMs / 1000;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("userId", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("name", user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
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
                ValidateIssuer = false,
                ValidateAudience = false,
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
