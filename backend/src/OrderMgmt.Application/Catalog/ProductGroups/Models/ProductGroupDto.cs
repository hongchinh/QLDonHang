using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.ProductGroups.Models;

public class ProductGroupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ProductGroupListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class CreateProductGroupRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateProductGroupRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ProductGroupListRequest : PageRequest
{
    public bool? IsActive { get; set; }
}
