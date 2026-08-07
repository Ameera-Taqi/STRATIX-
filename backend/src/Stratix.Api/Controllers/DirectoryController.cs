using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Users;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

/// <summary>Read-only tenant user directory for assignee/manager pickers.</summary>
[ApiController]
[Route("api/directory")]
[Authorize(Policy = AuthPolicies.AllTenantUsers)]
public class DirectoryController : ControllerBase
{
    private readonly IUserService _users;

    public DirectoryController(IUserService users) => _users = users;

    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResponse<UserDirectoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDirectoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        if (pageSize is > 0)
        {
            var paged = await _users.GetDirectoryPagedAsync(page ?? 1, pageSize.Value, ct);
            PagingHeaders.Apply(Response, paged);
            return Ok(paged.ToResponse());
        }

        return Ok(await _users.GetDirectoryAsync(ct));
    }
}
