using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Audit;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "ADMIN,PROJECT_MANAGER,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditTrailService _audit;

    public AuditLogsController(IAuditTrailService audit) => _audit = audit;

    [HttpGet]
    public async Task<AuditLogPageResponse> Search(
        [FromQuery] string? entityType,
        [FromQuery] long? entityId,
        [FromQuery] long? userId,
        [FromQuery] string? action,
        [FromQuery] long? projectId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] string? search,
        [FromQuery] int page = 0,
        [FromQuery] int size = 25,
        CancellationToken ct = default) =>
        await _audit.SearchAsync(new AuditLogQuery(entityType, entityId, userId, action, projectId, startDate, endDate, search, page, size), ct);
}
