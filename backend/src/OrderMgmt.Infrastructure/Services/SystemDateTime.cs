using OrderMgmt.Application.Common.Interfaces;

namespace OrderMgmt.Infrastructure.Services;

public class SystemDateTime : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset Now => DateTimeOffset.Now;
}
