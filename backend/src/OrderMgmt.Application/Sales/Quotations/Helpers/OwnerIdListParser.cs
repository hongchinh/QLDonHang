namespace OrderMgmt.Application.Sales.Quotations.Helpers;

internal static class OwnerIdListParser
{
    public static IReadOnlyList<Guid> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<Guid>();
        var result = new List<Guid>();
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(token, out var g)) result.Add(g);
        }
        return result;
    }

    public static bool IsValid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return true;
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(token, out _)) return false;
        }
        return true;
    }
}
