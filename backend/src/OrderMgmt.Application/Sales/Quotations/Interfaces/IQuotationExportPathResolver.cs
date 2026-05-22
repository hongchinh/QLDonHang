namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

using OrderMgmt.Application.Sales.Quotations.Models;

public interface IQuotationExportPathResolver
{
    /// <summary>
    /// Returns the absolute path to the Excel template to use for rendering the quotation
    /// owned by the given user. Falls back to the system template when the user has no
    /// per-user template or the file is missing.
    /// </summary>
    Task<string> ResolveTemplatePathAsync(Guid ownerUserId, CancellationToken ct = default);

    /// <summary>
    /// Returns the absolute path to the handover template for the given user and type.
    /// Falls back to the system template when the user has no per-user template or the file is missing.
    /// </summary>
    Task<string> ResolveHandoverTemplatePathAsync(
        Guid ownerUserId,
        QuotationTemplateType type,
        CancellationToken ct = default);

    /// <summary>
    /// Absolute path of the directory holding user-uploaded templates (creates it if missing).
    /// </summary>
    string GetUserTemplatesDirectory();
}
