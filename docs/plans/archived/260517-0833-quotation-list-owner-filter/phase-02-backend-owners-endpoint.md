# Phase 02 — Backend: endpoint owners + seed Accountant

**Status:** [ ] pending
**Complexity:** M

## Objective
Thêm endpoint `GET /api/quotations/owners?includeDeleted=true` trả danh sách user có quotation (gồm user đã xóa). Cấp `quotations.view_all` cho role Accountant trong seeder.

## Files
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` (thêm `QuotationOwnerOptionDto`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` (thêm `ListOwnersAsync`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (impl `ListOwnersAsync`)
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` (thêm action `ListOwners`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs` (sửa permission list của `RoleCodes.Accountant`)

## Tasks
1. **Thêm DTO** ở cuối `QuotationDto.cs`:
   ```csharp
   public class QuotationOwnerOptionDto
   {
       public Guid Id { get; set; }
       public string FullName { get; set; } = default!;
       public bool IsDeleted { get; set; }
       public int QuotationCount { get; set; }
   }
   ```

2. **Sửa `IQuotationService`**:
   ```csharp
   Task<IReadOnlyList<QuotationOwnerOptionDto>> ListOwnersAsync(bool includeDeleted, CancellationToken ct = default);
   ```

3. **Implement `ListOwnersAsync`** trong `QuotationService.cs` (vị trí: sau `ListAsync`):
   ```csharp
   public async Task<IReadOnlyList<QuotationOwnerOptionDto>> ListOwnersAsync(bool includeDeleted, CancellationToken ct = default)
   {
       // Group quotations theo OwnerUserId để có count.
       var ownerStats = await _db.Quotations
           .AsNoTracking()
           .Where(q => !q.IsDeleted)
           .GroupBy(q => q.OwnerUserId)
           .Select(g => new { OwnerUserId = g.Key, QuotationCount = g.Count() })
           .ToListAsync(ct);

       if (ownerStats.Count == 0) return Array.Empty<QuotationOwnerOptionDto>();

       var ownerIds = ownerStats.Select(s => s.OwnerUserId).ToList();
       var usersQuery = _db.Users.IgnoreQueryFilters()
           .AsNoTracking()
           .Where(u => ownerIds.Contains(u.Id));
       if (!includeDeleted)
           usersQuery = usersQuery.Where(u => !u.IsDeleted);

       var users = await usersQuery
           .Select(u => new { u.Id, u.FullName, u.IsDeleted })
           .ToListAsync(ct);

       return users
           .Join(ownerStats, u => u.Id, s => s.OwnerUserId, (u, s) => new QuotationOwnerOptionDto
           {
               Id = u.Id,
               FullName = u.FullName,
               IsDeleted = u.IsDeleted,
               QuotationCount = s.QuotationCount,
           })
           .OrderBy(o => o.IsDeleted)           // active trước
           .ThenBy(o => o.FullName, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), ignoreCase: true))
           .ToList();
   }
   ```

4. **Thêm action** trong `QuotationsController.cs` (vị trí: ngay sau `List` action, dòng ~38):
   ```csharp
   [HttpGet("owners")]
   [HasPermission(Permissions.Quotations.ViewAll)]
   [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuotationOwnerOptionDto>>), StatusCodes.Status200OK)]
   public async Task<ActionResult<ApiResponse<IReadOnlyList<QuotationOwnerOptionDto>>>> ListOwners(
       [FromQuery] bool includeDeleted, CancellationToken ct)
       => Success(await _quotations.ListOwnersAsync(includeDeleted, ct));
   ```

5. **Sửa seed Accountant** trong `DbSeeder.cs` ([DbSeeder.cs:124-129](../../backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs#L124-L129)):
   ```csharp
   (RoleCodes.Accountant, "Kế toán", new[]
   {
       Permissions.Customers.View, Permissions.Products.View,
       Permissions.Quotations.View,
       Permissions.Quotations.ViewAll,                     // NEW
       Permissions.Reports.Revenue, Permissions.Reports.Debt,
   }),
   ```
   Seeder đã idempotent (xem dòng 146-152) — chạy lại sẽ thêm `RolePermission` thiếu.

## Verification
```powershell
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
```
- Build xanh toàn bộ.
- **WebApi pickup**: route mới `/quotations/owners` + permission Accountant mới chỉ có hiệu lực sau khi process WebApi restart (seeder chạy lúc startup, route table build lúc startup). Trái với memory rule [[feedback_build_skip_when_app_running]] — phase này LÀ exception đã được pre-check với user (xem SUMMARY § Pre-execution checklist). Trước khi verify endpoint, đảm bảo executor đã chốt phương án A (bounce thủ công) hoặc B (`dotnet watch`). Tích hợp test ở Phase 03 dùng `WebAppFactory` nên seeder tự chạy — không bị ảnh hưởng.

## Exit Criteria
- [ ] `QuotationOwnerOptionDto` tồn tại với 4 field.
- [ ] `IQuotationService.ListOwnersAsync` được khai báo và implement đúng signature.
- [ ] `QuotationsController.ListOwners` action trả về `ApiResponse<IReadOnlyList<QuotationOwnerOptionDto>>`, có `[HasPermission(ViewAll)]`.
- [ ] `DbSeeder.SeedRolesAsync` permission list của Accountant chứa `Permissions.Quotations.ViewAll`.
- [ ] 3 project Application/Infrastructure/WebApi build xanh.
- [ ] Manual curl test (sau khi WebApi restart và seeder chạy):
  ```powershell
  # Login admin → lấy token → call
  curl -H "Authorization: Bearer <token>" "http://localhost:5xxx/api/quotations/owners?includeDeleted=true"
  # Expect: 200 với array; user deleted ở cuối.
  ```
