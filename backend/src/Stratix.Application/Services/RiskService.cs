using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Services;

namespace Stratix.Application.Services;

public class RiskService : IRiskService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;

    public RiskService(IApplicationDbContext db, IAuditTrailService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RiskResponse>> GetAllAsync(CancellationToken ct = default) =>
        await Query().OrderByDescending(r => r.CreatedAt).Select(r => EntityMappers.ToResponse(r)).ToListAsync(ct);

    public async Task<RiskResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindAsync(id, ct));

    public async Task<RiskResponse> CreateAsync(CreateRiskRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var risk = new ProjectRisk
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Impact = request.Impact,
            Probability = request.Probability,
            RiskLevel = RiskLevelCalculator.Calculate(request.Impact, request.Probability),
            MitigationPlan = request.MitigationPlan,
            Status = request.Status ?? RiskStatus.OPEN,
            ProjectId = request.ProjectId,
            OwnerId = request.OwnerId,
            CreatedAt = now,
            UpdatedAt = now
        };
        await ValidateRelationsAsync(risk, ct);
        _db.Add(risk);
        await _db.SaveChangesAsync(ct);
        risk = await FindAsync(risk.Id, ct);
        await _audit.RecordCreateAsync(AuditEntityType.RISK, risk.Id, risk.Title, null, $"Risk created: {risk.Title}", risk.ProjectId, risk.Project.Name, ct);
        return EntityMappers.ToResponse(risk);
    }

    public async Task<RiskResponse> UpdateAsync(long id, UpdateRiskRequest request, CancellationToken ct = default)
    {
        var risk = await FindAsync(id, ct);
        risk.Title = request.Title.Trim();
        risk.Description = request.Description;
        risk.Impact = request.Impact;
        risk.Probability = request.Probability;
        risk.RiskLevel = RiskLevelCalculator.Calculate(request.Impact, request.Probability);
        risk.MitigationPlan = request.MitigationPlan;
        risk.Status = request.Status;
        risk.ProjectId = request.ProjectId;
        risk.OwnerId = request.OwnerId;
        risk.UpdatedAt = DateTimeOffset.UtcNow;
        await ValidateRelationsAsync(risk, ct);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var risk = await FindAsync(id, ct);
        var projectId = risk.ProjectId;
        var projectName = risk.Project.Name;
        var title = risk.Title;
        _db.Remove(risk);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordDeleteAsync(AuditEntityType.RISK, id, title, null, $"Risk deleted: {title}", projectId, projectName, ct);
    }

    public async Task<RiskDashboardStatsResponse> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var risks = await _db.ProjectRisks.ToListAsync(ct);
        return new RiskDashboardStatsResponse(
            risks.Count,
            risks.Count(r => r.Status == RiskStatus.OPEN),
            risks.Count(r => r.RiskLevel == RiskLevel.CRITICAL),
            risks.Count(r => r.Status == RiskStatus.CLOSED));
    }

    public async Task<RiskHeatMapResponse> GetHeatMapAsync(CancellationToken ct = default)
    {
        var risks = await _db.ProjectRisks.ToListAsync(ct);
        var cells = new List<RiskHeatMapCell>();
        foreach (RiskImpact impact in Enum.GetValues<RiskImpact>())
        foreach (RiskProbability probability in Enum.GetValues<RiskProbability>())
        {
            var matching = risks.Where(r => r.Impact == impact && r.Probability == probability).ToList();
            if (matching.Count == 0) continue;
            cells.Add(new RiskHeatMapCell(impact, probability, matching[0].RiskLevel, matching.Count));
        }
        return new RiskHeatMapResponse(cells, risks.Count);
    }

    public async Task<IReadOnlyList<RiskResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);
        return await Query().Where(r => r.ProjectId == projectId).OrderByDescending(r => r.CreatedAt)
            .Select(r => EntityMappers.ToResponse(r)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RiskResponse>> GetByOwnerAsync(long ownerId, CancellationToken ct = default) =>
        await Query().Where(r => r.OwnerId == ownerId).OrderByDescending(r => r.CreatedAt)
            .Select(r => EntityMappers.ToResponse(r)).ToListAsync(ct);

    public async Task<IReadOnlyList<RiskResponse>> GetOpenAsync(CancellationToken ct = default) =>
        await Query().Where(r => r.Status == RiskStatus.OPEN).OrderByDescending(r => r.CreatedAt)
            .Select(r => EntityMappers.ToResponse(r)).ToListAsync(ct);

    public async Task<IReadOnlyList<RiskResponse>> GetCriticalAsync(CancellationToken ct = default) =>
        await Query().Where(r => r.RiskLevel == RiskLevel.CRITICAL).OrderByDescending(r => r.CreatedAt)
            .Select(r => EntityMappers.ToResponse(r)).ToListAsync(ct);

    private IQueryable<ProjectRisk> Query() =>
        _db.ProjectRisks.Include(r => r.Project).Include(r => r.Owner);

    private async Task<ProjectRisk> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new KeyNotFoundException("Risk not found");

    private async Task ValidateRelationsAsync(ProjectRisk risk, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == risk.ProjectId, ct))
            throw new ArgumentException("Project not found");
        if (!await _db.Users.AnyAsync(u => u.Id == risk.OwnerId, ct))
            throw new ArgumentException("Owner not found");
    }

    private async Task EnsureProjectExistsAsync(long projectId, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
            throw new KeyNotFoundException("Project not found");
    }
}
