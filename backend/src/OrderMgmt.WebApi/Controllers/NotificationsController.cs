using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Notifications.Interfaces;
using OrderMgmt.Application.Notifications.Models;

namespace OrderMgmt.WebApi.Controllers;

[Authorize]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _svc;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(INotificationService svc, ICurrentUser currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    private Guid CurrentUserId =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
        => Success(await _svc.ListAsync(CurrentUserId, unreadOnly, Math.Clamp(limit, 1, 50), ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> UnreadCount(CancellationToken ct)
        => Success(await _svc.CountUnreadAsync(CurrentUserId, ct));

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse>> MarkRead(Guid id, CancellationToken ct)
    {
        await _svc.MarkReadAsync(id, CurrentUserId, ct);
        return Success();
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult<ApiResponse>> MarkAllRead(CancellationToken ct)
    {
        await _svc.MarkAllReadAsync(CurrentUserId, ct);
        return Success();
    }
}
