using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Search.Models;

public sealed record QuotationSearchItemDto(
    Guid Id,
    string Code,
    string CustomerName,
    decimal Total,
    QuotationStatus Status,
    DateTimeOffset CreatedAt);
