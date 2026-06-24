using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    long GetExpirationSeconds();
    bool TryValidate(string token, out long userId, out UserRole role);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IPasswordResetMailService
{
    Task SendResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    long? UserId { get; }
    string? UserName { get; }
    UserRole? Role { get; }
}
