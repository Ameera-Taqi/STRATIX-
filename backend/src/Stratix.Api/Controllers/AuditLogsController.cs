using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Audit;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthPolicies.LeadersAndExecutives)]
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
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        [FromQuery] int? size = null,
        CancellationToken ct = default)
    {
        var resolvedSize = pageSize ?? size ?? PageQuery.DefaultPageSize;
        return await _audit.SearchAsync(
            new AuditLogQuery(entityType, entityId, userId, action, projectId, startDate, endDate, search, page, resolvedSize),
            ct);
    }
}
