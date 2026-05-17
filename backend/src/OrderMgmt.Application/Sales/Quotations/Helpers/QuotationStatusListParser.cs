using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Sales.Quotations.Helpers;

internal static class QuotationStatusListParser
{
    public static IReadOnlyList<QuotationStatus> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<QuotationStatus>();
        var result = new List<QuotationStatus>();
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<QuotationStatus>(token, ignoreCase: true, out var s))
                result.Add(s);
        }
        return result;
    }

    public static bool IsValid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return true;
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<QuotationStatus>(token, ignoreCase: true, out _))
                return false;
        }
        return true;
    }
}
