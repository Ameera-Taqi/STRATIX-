using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.EmployeeKpis;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;

namespace Stratix.Application.Services;

public class EmployeeKpiService : IEmployeeKpiService
{
    private readonly IApplicationDbContext _db;

    public EmployeeKpiService(IApplicationDbContext db) => _db = db;

    private IQueryable<EmployeeKpi> Query() => _db.EmployeeKpis.Include(k => k.User);

    public async Task<IReadOnlyList<EmployeeKpiResponse>> GetAllAsync(long? userId, CancellationToken ct = default)
    {
        var q = Query();
        if (userId.HasValue) q = q.Where(k => k.UserId == userId);
        return await q.OrderByDescending(k => k.Period).Select(k => EntityMappers.ToResponse(k)).ToListAsync(ct);
    }

    public async Task<EmployeeKpiResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindAsync(id, ct));

    public async Task<EmployeeKpiResponse> CreateAsync(CreateEmployeeKpiRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new ArgumentException("Employee not found");

        var period = request.Period.Trim();
        if (period.Length == 0)
            throw new ArgumentException("Period is required.");

        await EnsureUniquePeriodAsync(request.UserId, period, excludeId: null, ct);

        var now = DateTimeOffset.UtcNow;
        var entity = new EmployeeKpi
        {
            OrganizationId = user.OrganizationId,
            UserId = request.UserId,
            Period = period,
            TasksCompleted = request.TasksCompleted,
            TasksOnTime = request.TasksOnTime,
            Score = request.Score,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(entity.Id, ct));
    }

    public async Task<EmployeeKpiResponse> UpdateAsync(long id, UpdateEmployeeKpiRequest request, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        var period = request.Period.Trim();
        if (period.Length == 0)
            throw new ArgumentException("Period is required.");

        await EnsureUniquePeriodAsync(entity.UserId, period, excludeId: id, ct);

        entity.Period = period;
        entity.TasksCompleted = request.TasksCompleted;
        entity.TasksOnTime = request.TasksOnTime;
        entity.Score = request.Score;
        entity.Notes = request.Notes;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        _db.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureUniquePeriodAsync(long userId, string period, long? excludeId, CancellationToken ct)
    {
        var exists = await _db.EmployeeKpis.AnyAsync(k =>
            k.UserId == userId &&
            k.Period == period &&
            (!excludeId.HasValue || k.Id != excludeId.Value), ct);

        if (exists)
            throw new InvalidOperationException(
                $"A KPI evaluation already exists for this employee in period '{period}'.");
    }

    private async Task<EmployeeKpi> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(k => k.Id == id, ct)
        ?? throw new KeyNotFoundException("KPI record not found");
}
