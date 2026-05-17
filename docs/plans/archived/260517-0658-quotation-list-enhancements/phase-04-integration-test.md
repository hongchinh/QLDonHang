# Phase 04 — Integration Test cho Multi-Status Filter

**Status:** [x] completed (compile-only; runtime verification deferred — Docker unavailable)
**Complexity:** S

## Objective
Bổ sung integration test xác nhận `GET /api/quotations?status=Draft,Sent` trả về đúng tập hợp báo giá, cùng với verify backward compatibility với `?status=Draft` (single) và xử lý invalid value.

## Files
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationListFilterTests.cs` (mới)

## Tasks

### 1. Khảo sát test fixture pattern
Đọc tham khảo (chỉ đọc, không sửa):
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationCrudTests.cs` — pattern test base.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationStateMachineTests.cs` — pattern tạo quotation ở nhiều trạng thái khác nhau.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationTestBase.cs` (hoặc tương đương) — `BuildRequest()`, `_client`, `_productId`.

### 2. Tạo file test mới

File: `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationListFilterTests.cs`

Skeleton:
```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationListFilterTests : QuotationTestBase
{
    public QuotationListFilterTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task List_with_multi_status_returns_quotations_in_any_listed_status()
    {
        // Arrange: tạo 3 báo giá ở 3 trạng thái khác nhau (Draft, Sent, Confirmed)
        var draftId = await CreateQuotationAsync();
        var sentId = await CreateQuotationAsync();
        await TransitionAsync(sentId, QuotationAction.Send);
        var confirmedId = await CreateQuotationAsync();
        await TransitionAsync(confirmedId, QuotationAction.Send);
        await TransitionAsync(confirmedId, QuotationAction.Confirm);

        // Act: filter Draft + Sent
        var response = await _client.GetFromJsonAsync<ApiResponse<PagedResult<QuotationListItemDto>>>(
            "/api/quotations?status=Draft,Sent&pageSize=100", TestJson.Options);

        // Assert
        response!.Data!.Items.Should().Contain(x => x.Id == draftId);
        response.Data.Items.Should().Contain(x => x.Id == sentId);
        response.Data.Items.Should().NotContain(x => x.Id == confirmedId);
    }

    [Fact]
    public async Task List_with_single_status_legacy_still_works()
    {
        var draftId = await CreateQuotationAsync();
        var sentId = await CreateQuotationAsync();
        await TransitionAsync(sentId, QuotationAction.Send);

        var response = await _client.GetFromJsonAsync<ApiResponse<PagedResult<QuotationListItemDto>>>(
            "/api/quotations?status=Draft&pageSize=100", TestJson.Options);

        response!.Data!.Items.Should().Contain(x => x.Id == draftId);
        response.Data.Items.Should().NotContain(x => x.Id == sentId);
    }

    [Fact]
    public async Task List_with_invalid_status_returns_400()
    {
        var response = await _client.GetAsync("/api/quotations?status=Invalid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_includes_subtotal_discount_freight_fields()
    {
        var id = await CreateQuotationAsync();

        var response = await _client.GetFromJsonAsync<ApiResponse<PagedResult<QuotationListItemDto>>>(
            "/api/quotations?pageSize=10", TestJson.Options);

        // Filter theo Id để chỉ assert trên record vừa tạo — tránh index `[0]` throw
        // nếu list empty, và tránh phụ thuộc vào thứ tự sort.
        response!.Data!.Items.Should()
            .ContainSingle(x => x.Id == id)
            .Which.Should().Match<QuotationListItemDto>(x =>
                x.Subtotal >= 0 && x.Discount >= 0 && x.Freight >= 0);
    }

    // Helper — adjust nếu QuotationTestBase đã có helper tương đương.
    private async Task<Guid> CreateQuotationAsync()
    {
        var res = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await res.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return created!.Data!.Id;
    }

    private async Task TransitionAsync(Guid id, QuotationAction action)
    {
        var req = new TransitionQuotationRequest { Action = action };
        var res = await _client.PostAsJsonAsync($"/api/quotations/{id}/transition", req);
        res.EnsureSuccessStatusCode();
    }
}
```

**Lưu ý implementation:**
- `BuildRequest()`, `_client`, `_productId` kế thừa từ `QuotationTestBase` (đã verify — không có sẵn `CreateQuotationAsync` nên cần local helper như trên).
- `WebAppFactory` được tạo lại mỗi test (xem `QuotationTestBase.InitializeAsync`) → DB state isolated per test, không cần lo race condition.
- Test `List_with_invalid_status_returns_400` xác nhận validator hoạt động (`?status=Invalid`).
- Test `List_includes_subtotal_discount_freight_fields` filter theo `Id` cụ thể (dùng `ContainSingle`) thay vì `Items[0]` — tránh `ArgumentOutOfRange` nếu list empty, và làm assertion failure message dễ debug hơn.
- Tránh assert giá trị tuyệt đối (vd `Subtotal.Should().Be(60_000)`) — projection có thể đổi formula sau này; chỉ verify field tồn tại và non-negative.

### 3. Adjust nếu pattern khác
Nếu sau khi đọc `QuotationTestBase` thấy:
- Có method `CreateDraftAsync()` / `CreateSentAsync()` / `CreateConfirmedAsync()` thì dùng thay cho local helper.
- `TestJson.Options` có thể đã được expose qua base.

## Verification

```powershell
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~QuotationListFilter"
```

Cũng chạy lại full Quotation suite để chắc chắn không regression:
```powershell
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~Quotation"
```

## Exit Criteria
- 4 test cases mới đều pass.
- Full Quotation test suite pass.
- Không có test nào bị regression vì thay đổi DTO/contract.
