namespace OrderMgmt.Application.Common.Interfaces;

public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
    DateTimeOffset Now { get; }
}
