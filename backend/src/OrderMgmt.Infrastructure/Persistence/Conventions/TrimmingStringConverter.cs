using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OrderMgmt.Infrastructure.Persistence.Conventions;

/// <summary>
/// Trims leading/trailing whitespace on writes so callers don't have to .Trim() every input.
/// Reads pass through unchanged (well-formed data already lacks edge whitespace).
/// </summary>
public class TrimmingStringConverter : ValueConverter<string, string>
{
    public TrimmingStringConverter()
        : base(v => v.Trim(), v => v)
    {
    }
}
