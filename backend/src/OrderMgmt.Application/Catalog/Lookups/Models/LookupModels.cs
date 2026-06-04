namespace OrderMgmt.Application.Catalog.Lookups.Models;

public class LookupItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class GetOrCreateUnitRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(255)]
    public string Name { get; set; } = default!;
}
