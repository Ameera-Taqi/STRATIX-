namespace Stratix.Application.DTOs.ProjectFiles;

public record ProjectFileResponse(long Id, long ProjectId, string ProjectName, string FileName,
    string? Description, string? Category, string? ContentType, long SizeBytes, string Url,
    long UploadedById, string UploadedByName, DateTimeOffset CreatedAt);

public record CreateProjectFileRequest(
    long ProjectId,
    string FileName,
    string? Description,
    string? Category,
    string? ContentType,
    long SizeBytes,
    string Url);
