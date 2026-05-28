using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Catalog.ProductGroups.Interfaces;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Catalog;

namespace OrderMgmt.Application.Catalog.ProductGroups.Services;

public class ProductGroupService : IProductGroupService
{
    private const string CodePrefix = "NG";
    private const int MaxCreateAttempts = 5;

    private readonly IAppDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;

    public ProductGroupService(IAppDbContext db, IDateTime clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ProductGroupListItemDto>> ListAsync(
        ProductGroupListRequest request, CancellationToken ct = default)
    {
        var query = _db.ProductGroups.AsNoTracking().Where(g => !g.IsDeleted);

        if (request.IsActive.HasValue)
            query = query.Where(g => g.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLike(request.Search.Trim())}%";
            query = query.Where(g =>
                EF.Functions.ILike(g.Name, pattern) ||
                EF.Functions.ILike(g.Code, pattern));
        }

        query = (request.SortBy?.ToLowerInvariant(), request.SortDirection?.ToLowerInvariant()) switch
        {
            ("name", "desc") => query.OrderByDescending(g => g.Name),
            ("name", _)      => query.OrderBy(g => g.Name),
            ("code", "desc") => query.OrderByDescending(g => g.Code),
            ("code", _)      => query.OrderBy(g => g.Code),
            _                => query.OrderBy(g => g.SortOrder).ThenBy(g => g.Name),
        };

        var totalItems = await query.CountAsync(ct);

        var groups = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var groupIds = groups.Select(g => g.Id).ToList();
        var countsByGroup = await _db.Products
            .Where(p => p.ProductGroupId.HasValue && groupIds.Contains(p.ProductGroupId.Value))
            .GroupBy(p => p.ProductGroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        var items = groups.Select(g => new ProductGroupListItemDto
        {
            Id           = g.Id,
            Code         = g.Code,
            Name         = g.Name,
            Description  = g.Description,
            SortOrder    = g.SortOrder,
            IsActive     = g.IsActive,
            ProductCount = countsByGroup.TryGetValue(g.Id, out var c) ? c : 0,
        }).ToList();

        return new PagedResult<ProductGroupListItemDto>
        {
            Items      = items,
            Page       = request.Page,
            PageSize   = request.PageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<ProductGroupDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var g = await _db.ProductGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ProductGroup), id);

        return await MapToDtoAsync(g, ct);
    }

    public async Task<ProductGroupDto> CreateAsync(
        CreateProductGroupRequest request, CancellationToken ct = default)
    {
        var explicitCode = !string.IsNullOrWhiteSpace(request.Code);

        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            var code = explicitCode ? request.Code!.Trim() : await GenerateCodeAsync(ct);

            if (await _db.ProductGroups.AnyAsync(g => g.Code == code, ct))
            {
                if (explicitCode)
                    throw new ConflictException($"Mã nhóm '{code}' đã tồn tại.");
                continue;
            }

            var group = new ProductGroup
            {
                Code        = code,
                Name        = request.Name.Trim(),
                Description = request.Description?.Trim(),
                SortOrder   = request.SortOrder,
                IsActive    = request.IsActive,
            };

            _db.ProductGroups.Add(group);

            try
            {
                await _db.SaveChangesAsync(ct);
                return await GetAsync(group.Id, ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex) && !explicitCode && attempt < MaxCreateAttempts)
            {
                _db.Entry(group).State = EntityState.Detached;
            }
        }

        throw new ConflictException("Không thể tạo mã nhóm tự động sau nhiều lần thử. Vui lòng thử lại.");
    }

    public async Task<ProductGroupDto> UpdateAsync(
        Guid id, UpdateProductGroupRequest request, CancellationToken ct = default)
    {
        var group = await _db.ProductGroups
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ProductGroup), id);

        group.Name        = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.SortOrder   = request.SortOrder;
        group.IsActive    = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(group.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var group = await _db.ProductGroups
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ProductGroup), id);

        group.IsDeleted = true;
        group.DeletedAt = _clock.UtcNow;
        group.DeletedBy = _currentUser.UserId;

        // Nullify references on linked products (soft-delete does not trigger DB cascade).
        await _db.Products
            .Where(p => p.ProductGroupId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ProductGroupId, (Guid?)null), ct);

        await _db.SaveChangesAsync(ct);
    }

    private async Task<ProductGroupDto> MapToDtoAsync(ProductGroup g, CancellationToken ct)
    {
        var productCount = await _db.Products
            .CountAsync(p => p.ProductGroupId == g.Id && !p.IsDeleted, ct);

        return new ProductGroupDto
        {
            Id           = g.Id,
            Code         = g.Code,
            Name         = g.Name,
            Description  = g.Description,
            SortOrder    = g.SortOrder,
            IsActive     = g.IsActive,
            ProductCount = productCount,
            CreatedAt    = g.CreatedAt,
        };
    }

    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var date = _clock.Now.ToString("yyMMdd");
        var todayCount = await _db.ProductGroups
            .CountAsync(g => g.Code.StartsWith($"{CodePrefix}-{date}"), ct);
        return $"{CodePrefix}-{date}-{todayCount + 1:D4}";
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        var sqlState = inner?.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
        return sqlState == "23505";
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
