using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Users;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

/// <summary>User administration (full profiles + CRUD). Use /api/directory/users for pickers.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthPolicies.OrgAdmins)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        if (pageSize is > 0)
        {
            var paged = await _users.GetPagedAsync(page ?? 1, pageSize.Value, ct);
            PagingHeaders.Apply(Response, paged);
            return Ok(paged.ToResponse());
        }

        return Ok(await _users.GetAllAsync(ct));
    }

    [HttpGet("{id:long}")]
    public async Task<UserResponse> Get(long id, CancellationToken ct) => await _users.GetByIdAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:long}")]
    public async Task<UserResponse> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken ct) =>
        await _users.UpdateAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _users.DeleteAsync(id, ct);
        return NoContent();
    }
}
