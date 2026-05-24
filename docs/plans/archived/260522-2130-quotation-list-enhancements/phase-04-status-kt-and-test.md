# Phase 04 — Status KT xác nhận & Integration Test

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm `AccountingConfirmed` vào dropdown và mặc định của status filter, rồi bổ sung integration test verify `AdvancePayment` trong list response.

## Files

- `frontend/src/pages/quotations/quotation-list-page.tsx`
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationListFilterTests.cs`

## Tasks

### 4.1 `quotation-list-page.tsx` — `STATUS_OPTIONS`

Thêm option vào mảng `STATUS_OPTIONS`, **sau** `'Confirmed'` và **trước** `'Cancelled'`:

```ts
{ value: 'AccountingConfirmed', label: 'KT xác nhận' },
```

Kết quả:
```ts
const STATUS_OPTIONS: ReadonlyArray<{ value: QuotationStatus; label: string }> = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Sent', label: 'Đã gửi' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'AccountingConfirmed', label: 'KT xác nhận' },
  { value: 'Cancelled', label: 'Đã hủy' },
];
```

### 4.2 `quotation-list-page.tsx` — `DEFAULT_ACTIVE_STATUSES`

Thêm `'AccountingConfirmed'`:

```ts
const DEFAULT_ACTIVE_STATUSES: ReadonlyArray<QuotationStatus> = [
  'Draft', 'Sent', 'Confirmed', 'AccountingConfirmed',
];
```

### 4.3 Integration test — `QuotationListFilterTests.cs`

Thêm 2 test methods vào class `QuotationListFilterTests` (trước closing `}`):

```csharp
[Fact]
public async Task List_item_includes_advance_payment_field()
{
    // Tạo báo giá với advancePayment = 500_000
    var req = BuildRequest();
    req.AdvancePayment = 500_000m;
    var res = await _client.PostAsJsonAsync("/api/quotations", req);
    res.EnsureSuccessStatusCode();
    var created = await res.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
    var id = created!.Data!.Id;

    var listRes = await _client.GetFromJsonAsync<ApiResponse<QuotationListResult>>(
        "/api/quotations?pageSize=100", TestJson.Options);

    listRes!.Data!.Items.Should()
        .ContainSingle(x => x.Id == id)
        .Which.AdvancePayment.Should().Be(500_000m);
}

[Fact]
public async Task List_aggregates_sum_advance_payment()
{
    // 2 báo giá: advancePayment = 200_000 và 300_000 → tổng 500_000
    var req1 = BuildRequest();
    req1.AdvancePayment = 200_000m;
    req1.TaxRate = 0; req1.Discount = 0; req1.Freight = 0;
    await (await _client.PostAsJsonAsync("/api/quotations", req1)).EnsureSuccessStatusCodeAsync();

    var req2 = BuildRequest();
    req2.AdvancePayment = 300_000m;
    req2.TaxRate = 0; req2.Discount = 0; req2.Freight = 0;
    await (await _client.PostAsJsonAsync("/api/quotations", req2)).EnsureSuccessStatusCodeAsync();

    var listRes = await _client.GetFromJsonAsync<ApiResponse<QuotationListResult>>(
        "/api/quotations?status=Draft&pageSize=100", TestJson.Options);

    listRes!.Data!.Aggregates.AdvancePayment.Should().BeGreaterThanOrEqualTo(500_000m);
}
```

> **Lưu ý:** `EnsureSuccessStatusCodeAsync()` là extension method nếu có, hoặc dùng pattern `.EnsureSuccessStatusCode()` thông thường cho `HttpResponseMessage`.
>
> Kiểm tra lại `HttpResponseMessage` extension — nếu project chỉ có `EnsureSuccessStatusCode()` (sync), dùng:
> ```csharp
> (await _client.PostAsJsonAsync("/api/quotations", req1)).EnsureSuccessStatusCode();
> ```

## Verification

```bash
# TypeScript
cd frontend && npx tsc --noEmit

# Integration test mới
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~List_item_includes_advance_payment|FullyQualifiedName~List_aggregates_sum_advance_payment" \
  -v n
```

## Exit Criteria

- `STATUS_OPTIONS` có 5 option, bao gồm `AccountingConfirmed`
- `DEFAULT_ACTIVE_STATUSES` có 4 giá trị (Draft, Sent, Confirmed, AccountingConfirmed)
- 2 integration test mới pass
- TypeScript compile sạch
