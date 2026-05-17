# Phase 01 — Backend aggregates trong list response

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** M

## Objective
Mở rộng response của `GET /api/quotations` để trả về `aggregates: { subtotal, discount, freight, total }` tính trên **toàn bộ filter** (không bị giới hạn bởi page). Aggregate chạy cùng filter pipeline với `Items` (gồm owner-scope, statuses, customerId, date range, search).

**Cancelled handling:** mặc định loại trừ Cancelled khi user không filter status (financial convention). Khi user lọc explicit có `Cancelled` thì SUM tôn trọng filter (gồm cả Cancelled).

## Files
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` — thêm 2 class.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` — đổi return type của `ListAsync`.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` — thêm SUM query + đổi return type.
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` — đổi generic của `ActionResult` ở action `List`.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationListFilterTests.cs` — thêm 6 test case.

> **Note về line numbers:** trong các Task bên dưới, line numbers chỉ là **gợi ý vị trí** tại thời điểm viết plan. Khi execute, **search bằng tên method/class** (ví dụ `ListAsync`, `QuotationListItemDto`) thay vì paste theo line — code có thể đã shift do edit khác.

## Tasks

### 1. Thêm DTO class trong `Models/QuotationDto.cs`
Thêm sau class `QuotationListItemDto` (hiện ở dòng 89-108):

```csharp
public class QuotationListAggregates
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Freight { get; set; }
    public decimal Total { get; set; }
}

public class QuotationListResult : PagedResult<QuotationListItemDto>
{
    public QuotationListAggregates Aggregates { get; init; } = new();
}
```

Chú ý: `PagedResult<T>` dùng `init` accessors → `QuotationListResult` cũng phải dùng `init` để có thể set trong object initializer của subclass.

### 2. Cập nhật `IQuotationService.cs`
Đổi signature `ListAsync` (dòng 8):
```csharp
Task<QuotationListResult> ListAsync(QuotationListRequest request, CancellationToken ct = default);
```

### 3. Cập nhật `QuotationService.ListAsync`

- Đổi return type:
  ```csharp
  public async Task<QuotationListResult> ListAsync(...)
  ```
- Sau đoạn build filter (sau search `ILike`, trước switch `SortBy`), thêm SUM với **Cancelled exclusion logic**:
  ```csharp
  // Aggregate query loại trừ Cancelled khi user không filter status explicit.
  // Khi user filter status (kể cả chứa Cancelled), aggregate tôn trọng filter.
  var aggregateQuery = statuses.Count == 0
      ? query.Where(q => q.Status != QuotationStatus.Cancelled)
      : query;

  var aggregates = await aggregateQuery
      .GroupBy(_ => 1)
      .Select(g => new QuotationListAggregates
      {
          Subtotal = g.Sum(q => q.Subtotal),
          Discount = g.Sum(q => q.Discount),
          Freight  = g.Sum(q => q.Freight),
          Total    = g.Sum(q => q.Total),
      })
      .FirstOrDefaultAsync(ct) ?? new QuotationListAggregates();
  ```
  Đặt aggregate query **trước** `OrderBy`/`Skip`/`Take` để tránh ORDER BY thừa khi SUM. **Khi execute:** bật EF logging 1 lần (hoặc dùng `.ToQueryString()` trong debug) để xác nhận SQL không có ORDER BY thừa và dùng được index trên các cột filter (`OwnerId`, `Status`, `QuotationDate`).

- Đổi `return new PagedResult<QuotationListItemDto> { ... }` thành:
  ```csharp
  return new QuotationListResult
  {
      Items = items,
      Page = request.Page,
      PageSize = request.PageSize,
      TotalItems = totalItems,
      Aggregates = aggregates,
  };
  ```

### 4. Cập nhật `QuotationsController.cs` action `List`
Đổi return type:
```csharp
public async Task<ActionResult<ApiResponse<QuotationListResult>>> List(
    [FromQuery] QuotationListRequest request, CancellationToken ct)
```

### 5. Thêm 6 integration test trong `QuotationListFilterTests.cs`

Thêm `using` nếu cần (likely đã có). Pattern: deserialize bằng `ApiResponse<QuotationListResult>` (loại mới — JSON sẽ map cả `items` và `aggregates`).

Thêm helpers nếu cần:
- `CreateQuotationWithLineAsync(decimal qty, decimal price, decimal discount = 0, decimal freight = 0)` — tạo báo giá với 1 line để control subtotal/total. Có thể mở rộng `BuildRequest()` đã có trong `QuotationTestBase`.

6 test case:

**a) `List_aggregates_sum_across_all_filtered_records_not_just_current_page`**
- Tạo 25 báo giá (qty=1, price=100) → mỗi báo giá `subtotal=100, total=100` (giả sử taxRate=0, discount=0, freight=0).
- GET `/api/quotations?pageSize=10` → items.Count = 10, `aggregates.subtotal = 2500`, `aggregates.total = 2500`.

**b) `List_aggregates_respect_status_filter`**
- Tạo 3 báo giá `Draft`, 2 báo giá `Sent` (qty=1, price=100 mỗi báo).
- GET `?status=Draft` → `aggregates.subtotal = 300`.
- GET `?status=Sent` → `aggregates.subtotal = 200`.

**c) `List_aggregates_respect_owner_scope`**
- Tạo 2 báo giá bằng user A (không có `quotations.view_all`), 3 báo giá bằng user B.
- Auth as user A → `aggregates.subtotal` chỉ tính 2 báo giá của A.
- Pattern auth/user switch dùng theo `QuotationTestBase` hiện có.

**d) `List_empty_result_returns_zero_aggregates`**
- Filter không match (vd `?from=2030-01-01`) → items.Count = 0, `aggregates.subtotal = 0`, `aggregates.total = 0`.

**e) `List_aggregates_exclude_cancelled_when_no_status_filter`** *(thay test cũ)*
- Tạo 3 báo giá Draft (price=100 mỗi báo) + 2 báo giá Draft đã transition → Cancelled (price=100 mỗi báo).
- GET `/api/quotations?pageSize=100` (không filter status) → items.Count = 5, NHƯNG `aggregates.subtotal = 300` (chỉ 3 Draft active, không tính 2 Cancelled).

**f) `List_aggregates_include_cancelled_when_explicitly_filtered`** *(mới)*
- Tạo 3 báo giá Draft + 2 báo giá Cancelled.
- GET `?status=Cancelled` → items.Count = 2, `aggregates.subtotal = 200`.
- GET `?status=Cancelled,Draft` → items.Count = 5, `aggregates.subtotal = 500` (cả Cancelled lẫn Draft).

## Verification

```powershell
# Build các project đã sửa (không restart WebApi đang chạy)
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
dotnet build backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj

# Chạy test có liên quan
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj `
  --filter "FullyQualifiedName~QuotationListFilterTests" `
  --no-build
```

## Exit Criteria
- [ ] Tất cả test cũ trong `QuotationListFilterTests` vẫn pass (no regression).
- [ ] 6 test mới pass.
- [ ] `dotnet build` cho `OrderMgmt.Application` và `OrderMgmt.WebApi` thành công, không warning mới.
- [ ] Response JSON của `GET /api/quotations` chứa field `aggregates` ở cùng cấp với `items`, `page`, `pageSize`, `totalItems`.
- [ ] SQL của aggregate query đã verify (qua EF logging hoặc `.ToQueryString()`): không có `ORDER BY` thừa; dùng được index trên các cột filter.
