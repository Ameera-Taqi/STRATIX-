using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Users;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class UserService : IUserService
{
    private static readonly UserRole[] ProjectManagerAssignableRoles =
        { UserRole.EMPLOYEE, UserRole.TEAM_LEADER, UserRole.PROJECT_MANAGER, UserRole.EXECUTIVE_VIEWER };

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditTrailService _audit;
    private readonly IPlanLimitService _planLimits;
    private readonly ICurrentUserService _currentUser;

    public UserService(IApplicationDbContext db, IPasswordHasher passwordHasher, IAuditTrailService audit, IPlanLimitService planLimits, ICurrentUserService currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _planLimits = planLimits;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Users.Include(u => u.Department).OrderBy(u => u.Name)
            .Select(u => EntityMappers.ToResponse(u)).ToListAsync(ct);

    public async Task<Common.PagedResult<UserResponse>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (p, size) = Common.PageQuery.Normalize(page, pageSize);
        var query = _db.Users.Include(u => u.Department);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Name)
            .Skip((p - 1) * size).Take(size)
            .Select(u => EntityMappers.ToResponse(u)).ToListAsync(ct);
        return new Common.PagedResult<UserResponse>(items, total, p, size);
    }

    public async Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await FindAsync(id, ct);
        return EntityMappers.ToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        ValidateRoleAssignment(request.Role);
        await _planLimits.EnsureCanAddUserAsync(ct);
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), ct))
            throw new InvalidOperationException("Email already in use");

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Password = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            JobTitle = request.JobTitle,
            Status = request.Status ?? UserStatus.ACTIVE,
            DepartmentId = request.DepartmentId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Add(user);
        await _db.SaveChangesAsync(ct);
        await LoadDepartmentAsync(user, ct);
        await _audit.RecordCreateAsync(AuditEntityType.USER, user.Id, user.Name, null, $"User created: {user.Name}", null, null, ct);
        return EntityMappers.ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default)
    {
        ValidateRoleAssignment(request.Role);
        var user = await FindAsync(id, ct);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id, ct))
            throw new InvalidOperationException("Email already in use");

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Role = request.Role;
        user.JobTitle = request.JobTitle;
        user.Status = request.Status;
        user.DepartmentId = request.DepartmentId;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(user);
    }

    private async Task<User> FindAsync(long id, CancellationToken ct) =>
        await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id, ct)
        ?? throw new KeyNotFoundException("User not found");

    // Prevents privilege escalation: a caller can never grant a role more powerful than
    // (or otherwise disallowed for) their own role.
    private void ValidateRoleAssignment(UserRole targetRole)
    {
        var callerRole = _currentUser.Role;

        if (targetRole == UserRole.SUPER_ADMIN && callerRole != UserRole.SUPER_ADMIN)
            throw new UnauthorizedAccessException("Only a super admin can assign the SUPER_ADMIN role.");

        if (targetRole == UserRole.ORG_ADMIN && callerRole is not (UserRole.SUPER_ADMIN or UserRole.ORG_ADMIN))
            throw new UnauthorizedAccessException("Only a super admin or organization admin can assign the ORG_ADMIN role.");

        if (callerRole == UserRole.PROJECT_MANAGER && !ProjectManagerAssignableRoles.Contains(targetRole))
            throw new UnauthorizedAccessException("Project managers can only assign Employee, Team Leader, Project Manager, or Executive Viewer roles.");
    }

    private async Task LoadDepartmentAsync(User user, CancellationToken ct) =>
        await _db.Users.Where(u => u.Id == user.Id).Select(u => u.Department).LoadAsync(ct);
}
