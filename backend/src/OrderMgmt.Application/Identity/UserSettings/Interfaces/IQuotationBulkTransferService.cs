using OrderMgmt.Application.Identity.UserSettings.Models;

namespace OrderMgmt.Application.Identity.UserSettings.Interfaces;

public interface IQuotationBulkTransferService
{
    Task<BulkTransferResult> TransferAllAsync(Guid fromUserId, BulkTransferRequest request, CancellationToken ct = default);
}
