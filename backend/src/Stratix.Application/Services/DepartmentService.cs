using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Departments;
using Stratix.Application.Interfaces;

namespace Stratix.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IApplicationDbContext _db;

    public DepartmentService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DepartmentResponse>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Departments.OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name))
            .ToListAsync(ct);
}
