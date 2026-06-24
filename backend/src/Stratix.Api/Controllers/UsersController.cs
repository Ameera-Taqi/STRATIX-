using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Users;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IReadOnlyList<UserResponse>> GetAll(CancellationToken ct) => await _users.GetAllAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<UserResponse> Get(long id, CancellationToken ct) => await _users.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<UserResponse> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken ct) =>
        await _users.UpdateAsync(id, request, ct);
}
