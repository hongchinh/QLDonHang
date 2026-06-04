using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Catalog.Lookups.Interfaces;
using OrderMgmt.Application.Catalog.Lookups.Models;
using OrderMgmt.Application.Common.Interfaces;

namespace OrderMgmt.Application.Catalog.Lookups.Services;

public class CatalogLookupService : ICatalogLookupService
{
    private readonly IAppDbContext _db;

    public CatalogLookupService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LookupItemDto>> ListProductGroupsAsync(CancellationToken ct = default)
    {
        return await _db.ProductGroups
            .AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Select(g => new LookupItemDto { Id = g.Id, Code = g.Code, Name = g.Name })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LookupItemDto>> ListUnitsAsync(CancellationToken ct = default)
    {
        return await _db.Units
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new LookupItemDto { Id = u.Id, Code = u.Code, Name = u.Name })
            .ToListAsync(ct);
    }

    public async Task<LookupItemDto> GetOrCreateUnitAsync(string name, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        var existing = await _db.Units
            .Where(u => !u.IsDeleted && u.Name.ToLower() == trimmed.ToLower())
            .FirstOrDefaultAsync(ct);

        if (existing != null)
            return new LookupItemDto { Id = existing.Id, Code = existing.Code, Name = existing.Name };

        var unit = new Domain.Entities.Catalog.Unit
        {
            Code = trimmed.Length <= 50 ? trimmed : trimmed[..50],
            Name = trimmed,
            IsActive = true,
        };
        _db.Units.Add(unit);
        await _db.SaveChangesAsync(ct);

        return new LookupItemDto { Id = unit.Id, Code = unit.Code, Name = unit.Name };
    }
}
