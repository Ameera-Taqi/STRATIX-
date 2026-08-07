using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;
using Stratix.Domain.Enums;

namespace Stratix.Api.Middleware;

/// <summary>
/// Blocks authenticated callers whose organization is suspended/cancelled/expired
/// even if their JWT has not yet expired.
/// </summary>
public sealed class OrganizationAccessMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationAccessMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        OrganizationAccessService orgAccess,
        ICurrentUserService currentUser,
        IApplicationDbContext db)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true
            && currentUser.UserId is long userId
            && currentUser.Role != UserRole.SUPER_ADMIN)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, context.RequestAborted);
            if (user is null)
                throw new UnauthorizedAccessException("User is not authorized.");

            await orgAccess.EnsureCanAuthenticateAsync(user, context.RequestAborted);
        }

        await _next(context);
    }
}
