using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Notifications;

public record NotificationResponse(long Id, long UserId, string Title, string? Message,
    string Type, bool IsRead, string? Link, DateTimeOffset CreatedAt);

public record CreateNotificationRequest(long UserId, string Title, string? Message, NotificationType? Type, string? Link);
