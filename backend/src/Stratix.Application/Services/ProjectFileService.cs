using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.ProjectFiles;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;

namespace Stratix.Application.Services;

public class ProjectFileService : IProjectFileService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProjectFileService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private IQueryable<ProjectFile> Query() => _db.ProjectFiles.Include(f => f.Project).Include(f => f.UploadedBy);

    public async Task<IReadOnlyList<ProjectFileResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct)) throw new KeyNotFoundException("Project not found");
        return await Query().Where(f => f.ProjectId == projectId).OrderByDescending(f => f.CreatedAt)
            .Select(f => EntityMappers.ToResponse(f)).ToListAsync(ct);
    }

    public async Task<ProjectFileResponse> CreateAsync(CreateProjectFileRequest request, CancellationToken ct = default)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == request.ProjectId, ct)) throw new ArgumentException("Project not found");
        if (_currentUser.UserId is not long userId) throw new UnauthorizedAccessException("Not authenticated");
        var entity = new ProjectFile
        {
            ProjectId = request.ProjectId,
            FileName = request.FileName.Trim(),
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            Url = request.Url.Trim(),
            UploadedById = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await Query().FirstAsync(f => f.Id == entity.Id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await Query().FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new KeyNotFoundException("File not found");
        _db.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
