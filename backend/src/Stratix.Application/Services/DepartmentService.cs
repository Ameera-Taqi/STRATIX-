using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Departments;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;

namespace Stratix.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IApplicationDbContext _db;

    public DepartmentService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DepartmentResponse>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Departments.OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.Description))
            .ToListAsync(ct);

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (name.Length == 0) throw new ArgumentException("Department name is required.");
        if (await _db.Departments.AnyAsync(d => d.Name == name, ct))
            throw new InvalidOperationException("A department with this name already exists.");

        var department = new Department
        {
            Name = name,
            Description = request.Description?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Add(department);
        await _db.SaveChangesAsync(ct);
        return new DepartmentResponse(department.Id, department.Name, department.Description);
    }

    public async Task<DepartmentResponse> UpdateAsync(long id, UpdateDepartmentRequest request, CancellationToken ct = default)
    {
        var department = await FindAsync(id, ct);
        var name = request.Name.Trim();
        if (name.Length == 0) throw new ArgumentException("Department name is required.");
        if (await _db.Departments.AnyAsync(d => d.Name == name && d.Id != id, ct))
            throw new InvalidOperationException("A department with this name already exists.");

        department.Name = name;
        department.Description = request.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        return new DepartmentResponse(department.Id, department.Name, department.Description);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var department = await FindAsync(id, ct);

        if (await _db.Users.AnyAsync(u => u.DepartmentId == id, ct))
            throw new InvalidOperationException("Cannot delete a department that still has users assigned to it.");
        if (await _db.Projects.AnyAsync(p => p.DepartmentId == id, ct))
            throw new InvalidOperationException("Cannot delete a department that still has projects assigned to it.");

        _db.Remove(department);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Department> FindAsync(long id, CancellationToken ct) =>
        await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
        ?? throw new KeyNotFoundException("Department not found");
}
