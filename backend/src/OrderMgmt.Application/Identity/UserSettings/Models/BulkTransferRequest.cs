namespace OrderMgmt.Application.Identity.UserSettings.Models;

public class BulkTransferRequest
{
    public Guid ToUserId { get; set; }
    public bool IncludeCancelled { get; set; } = false;
    public string? Reason { get; set; }
}

public class BulkTransferResult
{
    public int AffectedCount { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
}
