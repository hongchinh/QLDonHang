namespace OrderMgmt.Application.Catalog.Lookups.Models;

public class LookupItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}
