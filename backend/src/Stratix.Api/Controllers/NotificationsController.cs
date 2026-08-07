using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Notifications;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service) => _service = service;

    // The caller's own notifications.
    [HttpGet]
    public async Task<IReadOnlyList<NotificationResponse>> GetMine(CancellationToken ct) => await _service.GetMineAsync(ct);

    [HttpPost]
    [Authorize(Policy = AuthPolicies.TeamLeaders)]
    public async Task<ActionResult<NotificationResponse>> Create([FromBody] CreateNotificationRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _service.MarkReadAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _service.MarkAllReadAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
