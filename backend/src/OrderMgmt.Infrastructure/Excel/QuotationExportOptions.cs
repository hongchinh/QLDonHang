namespace OrderMgmt.Infrastructure.Excel;

public class QuotationExportOptions
{
    public const string SectionName = "QuotationExport";
    public string TemplatePath { get; set; } = string.Empty;
    public string LibreOfficePath { get; set; } = string.Empty;
    public int ConversionTimeoutSeconds { get; set; } = 60;
    public string HandoverWithPriceTemplatePath { get; set; } = string.Empty;
    public string HandoverNoPriceTemplatePath { get; set; } = string.Empty;

    public string UserTemplatesPath { get; set; } = "templates/users";
    public long UploadMaxBytes { get; set; } = 5 * 1024 * 1024;
    public long UnzippedMaxBytes { get; set; } = 50 * 1024 * 1024;
    public string[] AllowedMimeTypes { get; set; } =
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
}
