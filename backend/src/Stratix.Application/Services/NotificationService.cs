using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Notifications;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetMineAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not long userId) throw new UnauthorizedAccessException("Not authenticated");
        return await _db.Notifications.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => EntityMappers.ToResponse(n)).ToListAsync(ct);
    }

    public async Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new ArgumentException("Recipient not found");
        var entity = new Notification
        {
            OrganizationId = user.OrganizationId,
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Message = request.Message,
            Type = request.Type ?? NotificationType.INFO,
            Link = request.Link,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(entity);
    }

    public async Task MarkReadAsync(long id, CancellationToken ct = default)
    {
        var n = await FindMineAsync(id, ct);
        if (!n.IsRead) { n.IsRead = true; await _db.SaveChangesAsync(ct); }
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not long userId) throw new UnauthorizedAccessException("Not authenticated");
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        foreach (var n in unread) n.IsRead = true;
        if (unread.Count > 0) await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var n = await FindMineAsync(id, ct);
        _db.Remove(n);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Notification> FindMineAsync(long id, CancellationToken ct)
    {
        if (_currentUser.UserId is not long userId) throw new UnauthorizedAccessException("Not authenticated");
        return await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Notification not found");
    }
}
