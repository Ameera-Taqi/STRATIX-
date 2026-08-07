using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Events;
using Stratix.Domain.Services;

namespace Stratix.Application.Services;

public class RiskService : IRiskService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;
    private readonly TenantRelationGuard _tenantGuard;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _events;

    public RiskService(
        IApplicationDbContext db,
        IAuditTrailService audit,
        TenantRelationGuard tenantGuard,
        ICurrentUserService currentUser,
        IDomainEventDispatcher events)
    {
        _db = db;
        _audit = audit;
        _tenantGuard = tenantGuard;
        _currentUser = currentUser;
        _events = events;
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
        await _audit.RecordCreateAsync(
            AuditEntityType.RISK,
            risk.Id,
            risk.Title,
            AuditSnapshot.Serialize(Snapshot(risk)),
            $"Risk created: {risk.Title}",
            risk.ProjectId,
            risk.Project.Name,
            ct);

        await _events.DispatchAsync(new RiskRaisedEvent(
            risk.OrganizationId,
            risk.ProjectId,
            risk.Project.Name,
            risk.Id,
            risk.Title,
            risk.RiskLevel,
            risk.OwnerId,
            risk.Project.ProjectManagerId,
            _currentUser.UserId ?? 0,
            _currentUser.UserName ?? "",
            DateTimeOffset.UtcNow), ct);

        return EntityMappers.ToResponse(risk);
    }

    public async Task<RiskResponse> UpdateAsync(long id, UpdateRiskRequest request, CancellationToken ct = default)
    {
        var risk = await FindAsync(id, ct);

        if (request.Status == RiskStatus.CLOSED && risk.Status != RiskStatus.CLOSED)
            throw new ArgumentException(
                "Use Close Risk with a resolution / closure reason. Status cannot be set to CLOSED via update.");

        var oldValues = AuditSnapshot.Serialize(Snapshot(risk));
        risk.Title = request.Title.Trim();
        risk.Description = request.Description;
        risk.Impact = request.Impact;
        risk.Probability = request.Probability;
        risk.RiskLevel = RiskLevelCalculator.Calculate(request.Impact, request.Probability);
        risk.MitigationPlan = request.MitigationPlan;
        // Closing requires CloseAsync with a resolution reason.
        if (request.Status != RiskStatus.CLOSED)
        {
            risk.Status = request.Status;
            risk.ClosureReason = null;
            risk.ResidualRisk = null;
        }
        risk.ProjectId = request.ProjectId;
        risk.OwnerId = request.OwnerId;
        risk.UpdatedAt = DateTimeOffset.UtcNow;
        await ValidateRelationsAsync(risk, ct);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordUpdateAsync(
            AuditEntityType.RISK,
            risk.Id,
            risk.Title,
            oldValues,
            AuditSnapshot.Serialize(Snapshot(risk)),
            $"Risk updated: {risk.Title}",
            risk.ProjectId,
            risk.Project.Name,
            ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task<RiskResponse> CloseAsync(long id, CloseRiskRequest request, CancellationToken ct = default)
    {
        var reason = request.ClosureReason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Resolution / closure reason is required when closing a risk.");

        var risk = await FindAsync(id, ct);
        if (risk.Status == RiskStatus.CLOSED)
            throw new InvalidOperationException("Risk is already closed.");

        var oldValues = AuditSnapshot.Serialize(Snapshot(risk));
        risk.Status = RiskStatus.CLOSED;
        risk.ClosureReason = reason;
        risk.ResidualRisk = request.ResidualRisk;
        risk.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.RecordUpdateAsync(
            AuditEntityType.RISK,
            risk.Id,
            risk.Title,
            oldValues,
            AuditSnapshot.Serialize(Snapshot(risk)),
            $"Risk closed: {risk.Title}",
            risk.ProjectId,
            risk.Project.Name,
            ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var risk = await FindAsync(id, ct);
        var projectId = risk.ProjectId;
        var projectName = risk.Project.Name;
        var title = risk.Title;
        var snapshot = AuditSnapshot.Serialize(Snapshot(risk));
        _db.Remove(risk);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordDeleteAsync(AuditEntityType.RISK, id, title, snapshot, $"Risk deleted: {title}", projectId, projectName, ct);
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

    private static object Snapshot(ProjectRisk r) => new
    {
        r.Title,
        r.Description,
        Impact = r.Impact.ToString(),
        Probability = r.Probability.ToString(),
        RiskLevel = r.RiskLevel.ToString(),
        r.MitigationPlan,
        Status = r.Status.ToString(),
        r.ClosureReason,
        ResidualRisk = r.ResidualRisk?.ToString(),
        r.ProjectId,
        r.OwnerId
    };

    private IQueryable<ProjectRisk> Query() =>
        _db.ProjectRisks.Include(r => r.Project).Include(r => r.Owner);

    private async Task<ProjectRisk> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new KeyNotFoundException("Risk not found");

    private async Task ValidateRelationsAsync(ProjectRisk risk, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == risk.ProjectId, ct)
            ?? throw new ArgumentException("Project not found");
        risk.OrganizationId = project.OrganizationId;
        await _tenantGuard.EnsureUserRequiredAsync(risk.OwnerId, project.OrganizationId, ct);
    }

    private async Task EnsureProjectExistsAsync(long projectId, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
            throw new KeyNotFoundException("Project not found");
    }
}
