# Phase 03 — Backend: integration tests

**Status:** [ ] pending
**Complexity:** M

## Objective
Mở rộng `QuotationListFilterTests.cs` với 5 test owner-filter, và tạo file mới `QuotationOwnersEndpointTests.cs` cho 4 test endpoint owners. Validate guard logic, parse logic, aggregates regression.

## Files
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationListFilterTests.cs` (mở rộng)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationOwnersEndpointTests.cs` (mới)

## Tasks

### A. Mở rộng `QuotationListFilterTests.cs`
Thêm 5 test sau (đặt nối tiếp các test hiện có, trước `private` helpers):

1. **Admin filter owner trả union 2 owner đúng**:
   - Tạo 2 báo giá bằng admin (owner = admin).
   - Tạo user `sale_owner_filter_a` (SALES), authen, tạo 3 báo giá.
   - Tạo user `sale_owner_filter_b` (SALES), authen, tạo 2 báo giá.
   - Authen lại `admin`. Lấy `ownerIdA`, `ownerIdB` qua DB scope (giống pattern `CreateTestUserAsync` trả qua scope).
   - Call `/api/quotations?ownerUserIds=<ownerIdA>,<ownerIdB>&pageSize=100`.
   - Expect: items count = 5, aggregates.subtotal đúng = (3+2)*100.

2. **Admin không truyền `ownerUserIds` → behavior cũ** (regression):
   - Tạo 2 báo giá admin + 2 sale báo giá.
   - Authen admin, call `/api/quotations?pageSize=100`.
   - Expect: 4 items.

3. **Sale forge `ownerUserIds` → filter ignored, vẫn chỉ thấy own**:
   - Tạo admin 1 báo giá (adminQuoteId).
   - Tạo `sale_forge` (SALES), authen, tạo 2 báo giá.
   - Lấy `adminUserId` (qua DB scope helper).
   - Sale call `/api/quotations?ownerUserIds=<adminUserId>&pageSize=100`.
   - Expect: 2 items (báo giá của sale), KHÔNG có adminQuoteId; status 200 (không 403).

4. **Validator: `ownerUserIds=not-a-guid` → 400**:
   - Authen admin, call `/api/quotations?ownerUserIds=abc,def`.
   - Expect: HttpStatusCode.BadRequest.

5. **Aggregates respect owner filter** (regression cho aggregate calc):
   - Tạo admin 3 báo giá (mỗi báo subtotal=100).
   - Tạo `sale_agg_owner` (SALES), authen, tạo 2 báo giá (subtotal=100 mỗi báo).
   - Authen admin. Lấy `saleAggOwnerId`.
   - Call `/api/quotations?ownerUserIds=<saleAggOwnerId>&pageSize=100`.
   - Expect: items=2, aggregates.subtotal=200 (KHÔNG phải 500).

**Helper cần thêm** trong test class (nếu chưa có):
```csharp
private async Task<Guid> GetUserIdAsync(string username)
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    return await db.Users.Where(u => u.Username == username).Select(u => u.Id).FirstAsync();
}
```

### B. Tạo `QuotationOwnersEndpointTests.cs` mới
Pattern theo các file test hiện có (`[Collection(nameof(PostgresCollection))]`, extends `QuotationTestBase`):

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationOwnersEndpointTests : QuotationTestBase
{
    public QuotationOwnersEndpointTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Owners_returns_only_users_with_quotations_active_only()
    {
        // admin tạo 2 báo giá; saleA tạo 1; saleB không tạo.
        await CreateSimpleQuotationAsync(1, 100);
        await CreateSimpleQuotationAsync(1, 100);
        await CreateTestUserAsync("sale_owners_a", "Sale@123", "SALES");
        await CreateTestUserAsync("sale_owners_b", "Sale@123", "SALES");
        await AuthenticateAsync("sale_owners_a", "Sale@123");
        await CreateSimpleQuotationAsync(1, 100);
        await AuthenticateAsync("admin", "Admin@123");

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=false", TestJson.Options);

        res!.Data.Should().HaveCount(2);  // admin + sale_owners_a
        res.Data.Should().Contain(o => o.FullName.Contains("admin", StringComparison.OrdinalIgnoreCase));
        res.Data.Should().Contain(o => o.FullName == "Test SALES");
        res.Data.Should().NotContain(o => o.IsDeleted);
    }

    [Fact]
    public async Task Owners_with_includeDeleted_returns_orphan_users_at_end()
    {
        await CreateTestUserAsync("sale_to_delete", "Sale@123", "SALES");
        await AuthenticateAsync("sale_to_delete", "Sale@123");
        await CreateSimpleQuotationAsync(1, 100);
        await AuthenticateAsync("admin", "Admin@123");
        await CreateSimpleQuotationAsync(1, 100);

        // Soft-delete sale user.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.FirstAsync(x => x.Username == "sale_to_delete");
            u.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=true", TestJson.Options);

        res!.Data.Should().HaveCount(2);
        res.Data.Last().IsDeleted.Should().BeTrue();  // deleted xuống cuối
    }

    [Fact]
    public async Task Owners_with_includeDeleted_false_excludes_orphan()
    {
        await CreateTestUserAsync("sale_excluded", "Sale@123", "SALES");
        await AuthenticateAsync("sale_excluded", "Sale@123");
        await CreateSimpleQuotationAsync(1, 100);
        await AuthenticateAsync("admin", "Admin@123");
        await CreateSimpleQuotationAsync(1, 100);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.FirstAsync(x => x.Username == "sale_excluded");
            u.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=false", TestJson.Options);

        res!.Data.Should().HaveCount(1);  // chỉ admin
        res.Data.Should().NotContain(o => o.IsDeleted);
    }

    [Fact]
    public async Task Owners_without_view_all_returns_403()
    {
        await CreateTestUserAsync("sale_no_perm", "Sale@123", "SALES");
        await AuthenticateAsync("sale_no_perm", "Sale@123");

        var res = await _client.GetAsync("/api/quotations/owners?includeDeleted=true");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owners_accountant_can_call_after_seed()
    {
        // Accountant role có view_all sau phase 02. Tạo accountant + verify call thành công.
        await CreateTestUserAsync("acc_owner", "Acc@123", "ACCOUNTANT");
        await CreateSimpleQuotationAsync(1, 100);  // admin tạo 1 báo giá
        await AuthenticateAsync("acc_owner", "Acc@123");

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=true", TestJson.Options);

        res!.Data.Should().NotBeEmpty();
    }

    // Reuse helper CreateSimpleQuotationAsync giống QuotationListFilterTests:
    private async Task<Guid> CreateSimpleQuotationAsync(decimal quantity, decimal unitPrice)
    {
        var req = BuildRequest(new UpsertQuotationLineRequest
        {
            SortOrder = 0,
            ProductId = _productId,
            ProductName = "Test EPS 1000x2000",
            UnitName = "Tấm",
            PricingMode = PricingMode.PerUnit,
            Quantity = quantity,
            UnitPrice = unitPrice,
        });
        req.TaxRate = 0; req.Discount = 0; req.Freight = 0;

        var res = await _client.PostAsJsonAsync("/api/quotations", req);
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return created!.Data!.Id;
    }
}
```

## Verification
```powershell
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj `
  --filter "FullyQualifiedName~QuotationListFilterTests|FullyQualifiedName~QuotationOwnersEndpointTests"
```
- Tất cả test cũ + 5 test mới ở `QuotationListFilterTests` PASS.
- 5 test mới ở `QuotationOwnersEndpointTests` PASS.
- Tổng số test PASS, 0 fail/skip.

## Exit Criteria
- [ ] 5 test mới được thêm vào `QuotationListFilterTests.cs`.
- [ ] File `QuotationOwnersEndpointTests.cs` mới với 5 test.
- [ ] Tất cả test trong namespace `OrderMgmt.IntegrationTests.Quotations` PASS.
- [ ] Không sửa test fixture base (`QuotationTestBase`, `PostgresFixture`) trừ khi thêm helper `GetUserIdAsync` (nếu cần dùng nhiều thì đưa vào base, không thì inline).
