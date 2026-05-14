namespace OrderMgmt.Infrastructure.Excel;

public class QuotationExportOptions
{
    public const string SectionName = "QuotationExport";
    public string TemplatePath { get; set; } = string.Empty;
    public string LibreOfficePath { get; set; } = string.Empty;
    public int ConversionTimeoutSeconds { get; set; } = 60;
}
