namespace OrderMgmt.Application.Sales.Quotations.Models;

public static class RevenueDateField
{
    public const string QuotationDate = "QuotationDate";
    public const string ConfirmedAt = "ConfirmedAt";
    public const string AccountingConfirmedAt = "AccountingConfirmedAt";

    public static readonly string[] AllowedValues = [QuotationDate, ConfirmedAt, AccountingConfirmedAt];
}

public class QuotationSystemSettingsDto
{
    public string RevenueReportingDateField { get; set; } = RevenueDateField.QuotationDate;
    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }
}

public class UpdateQuotationSystemSettingsRequest
{
    public string RevenueReportingDateField { get; set; } = "QuotationDate";
}
