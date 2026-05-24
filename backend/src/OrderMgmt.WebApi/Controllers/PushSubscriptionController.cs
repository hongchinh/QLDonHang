using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Notifications;

namespace OrderMgmt.WebApi.Controllers;

[Authorize]
[Route("api/push")]
public class PushSubscriptionController : ApiControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PushSubscriptionController(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid CurrentUserId =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated.");

    [HttpPost("subscribe")]
    public async Task<ActionResult<ApiResponse>> Subscribe(
        [FromBody] SubscribeRequest request,
        CancellationToken ct)
    {
        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);

        if (existing is not null)
        {
            if (existing.UserId != CurrentUserId)
                _db.PushSubscriptions.Remove(existing);
            else
            {
                existing.P256DH = request.P256DH;
                existing.Auth = request.Auth;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
                return Success();
            }
        }

        _db.PushSubscriptions.Add(new PushSubscription
        {
            UserId = CurrentUserId,
            Endpoint = request.Endpoint,
            P256DH = request.P256DH,
            Auth = request.Auth,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return Success();
    }

    [HttpDelete("subscribe")]
    public async Task<ActionResult<ApiResponse>> Unsubscribe(
        [FromBody] UnsubscribeRequest request,
        CancellationToken ct)
    {
        var sub = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint && s.UserId == CurrentUserId, ct);

        if (sub is not null)
        {
            _db.PushSubscriptions.Remove(sub);
            await _db.SaveChangesAsync(ct);
        }

        return Success();
    }

    public record SubscribeRequest(
        [Required, MaxLength(2048)] string Endpoint,
        [Required, MaxLength(512)] string P256DH,
        [Required, MaxLength(256)] string Auth);

    public record UnsubscribeRequest([Required, MaxLength(2048)] string Endpoint);
}
