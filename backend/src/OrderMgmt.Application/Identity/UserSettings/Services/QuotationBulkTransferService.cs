using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Identity.UserSettings.Interfaces;
using OrderMgmt.Application.Identity.UserSettings.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Identity;
using OrderMgmt.Domain.Entities.Sales;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Identity.UserSettings.Services;

public class QuotationBulkTransferService : IQuotationBulkTransferService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly ILogger<QuotationBulkTransferService> _logger;

    public QuotationBulkTransferService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDateTime clock,
        ILogger<QuotationBulkTransferService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<BulkTransferResult> TransferAllAsync(Guid fromUserId, BulkTransferRequest request, CancellationToken ct = default)
    {
        if (fromUserId == request.ToUserId)
            throw new DomainException("VALIDATION", "User nhận không được trùng với user nguồn.");

        var actorId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        // Allow toUser to be a previously soft-deleted record? Plan: ToUser must be Active.
        var toUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.ToUserId, ct)
            ?? throw new NotFoundException(nameof(User), request.ToUserId);
        if (toUser.Status != UserStatus.Active)
            throw new DomainException("VALIDATION", "User nhận đang bị vô hiệu hoá.");

        var query = _db.Quotations
            .Where(q => q.OwnerUserId == fromUserId && !q.IsDeleted);
        if (!request.IncludeCancelled)
            query = query.Where(q => q.Status != QuotationStatus.Cancelled);

        var quotations = await query.ToListAsync(ct);
        if (quotations.Count == 0)
        {
            return new BulkTransferResult
            {
                AffectedCount = 0,
                FromUserId = fromUserId,
                ToUserId = request.ToUserId,
            };
        }

        var now = _clock.UtcNow;
        foreach (var q in quotations)
        {
            var oldOwnerUserId = q.OwnerUserId;
            _db.QuotationOwnerHistory.Add(new QuotationOwnerHistory
            {
                QuotationId = q.Id,
                OldOwnerUserId = oldOwnerUserId,
                NewOwnerUserId = request.ToUserId,
                ActorUserId = actorId,
                Reason = request.Reason,
                ChangedAt = now,
            });
            _db.QuotationActivities.Add(new QuotationActivity
            {
                QuotationId = q.Id,
                Action = QuotationActivityAction.OwnerTransferred,
                ActorUserId = actorId,
                OccurredAt = now,
                Description = "Chuyển chủ sở hữu báo giá hàng loạt",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    OldOwnerUserId = oldOwnerUserId,
                    NewOwnerUserId = request.ToUserId,
                    request.Reason,
                    Bulk = true,
                }),
            });
            q.OwnerUserId = request.ToUserId;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Bulk transfer: From={From}, To={To}, Count={Count}, Actor={Actor}",
            fromUserId, request.ToUserId, quotations.Count, actorId);

        return new BulkTransferResult
        {
            AffectedCount = quotations.Count,
            FromUserId = fromUserId,
            ToUserId = request.ToUserId,
        };
    }
}
