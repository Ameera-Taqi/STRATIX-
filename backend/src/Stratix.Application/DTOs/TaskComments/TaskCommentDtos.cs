namespace Stratix.Application.DTOs.TaskComments;

public record TaskCommentResponse(long Id, long TaskId, long UserId, string UserName, string Comment, DateTimeOffset CreatedAt);

public record CreateTaskCommentRequest(long TaskId, string Comment);
