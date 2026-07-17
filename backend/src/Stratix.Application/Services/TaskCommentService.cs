using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.TaskComments;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class TaskCommentService : ITaskCommentService
{
    private static readonly UserRole[] ModeratorRoles =
        { UserRole.SUPER_ADMIN, UserRole.ORG_ADMIN, UserRole.ADMIN };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TaskCommentService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private IQueryable<TaskComment> Query() => _db.TaskComments.Include(c => c.User);

    public async Task<IReadOnlyList<TaskCommentResponse>> GetByTaskAsync(long taskId, CancellationToken ct = default)
    {
        if (!await _db.Tasks.AnyAsync(t => t.Id == taskId, ct))
            throw new KeyNotFoundException("Task not found");

        return await Query().Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt)
            .Select(c => EntityMappers.ToResponse(c)).ToListAsync(ct);
    }

    public async Task<TaskCommentResponse> CreateAsync(CreateTaskCommentRequest request, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not long userId)
            throw new UnauthorizedAccessException("Not authenticated");
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new ArgumentException("Comment text is required.");
        if (!await _db.Tasks.AnyAsync(t => t.Id == request.TaskId, ct))
            throw new ArgumentException("Task not found");

        var comment = new TaskComment
        {
            TaskId = request.TaskId,
            UserId = userId,
            Comment = request.Comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Add(comment);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await Query().FirstAsync(c => c.Id == comment.Id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var comment = await _db.TaskComments.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Comment not found");

        var isAuthor = _currentUser.UserId == comment.UserId;
        var isModerator = _currentUser.Role.HasValue && ModeratorRoles.Contains(_currentUser.Role.Value);
        if (!isAuthor && !isModerator)
            throw new UnauthorizedAccessException("You can only delete your own comments.");

        _db.Remove(comment);
        await _db.SaveChangesAsync(ct);
    }
}
