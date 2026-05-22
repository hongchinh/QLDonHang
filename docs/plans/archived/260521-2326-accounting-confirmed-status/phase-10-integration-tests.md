# Phase 10 — Integration Tests

**Status:** [ ] pending
**Complexity:** M

## Objective

Bổ sung integration tests để cover state machine mới, permission gates, và timestamp behavior của `AccountingConfirmed`.

## Files

- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationStateMachineTests.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationTestBase.cs` (nếu cần helper mới)
- `backend/tests/OrderMgmt.IntegrationTests/Settings/QuotationSystemSettingsTests.cs` ← file mới

## Tasks

### Helpers trong `QuotationTestBase.cs` (nếu chưa có)

1. Kiểm tra `QuotationTestBase` xem đã có helper `TransitionToConfirmedAsync` chưa. Nếu chưa, thêm:
   ```csharp
   protected async Task<QuotationDto> TransitionToConfirmedAsync(Guid id)
   {
       await TransitionAsync(id, QuotationAction.Send);
       return await TransitionAsync(id, QuotationAction.Confirm);
   }
   ```

### Tests trong `QuotationStateMachineTests.cs`

2. **Test: Confirmed → AccountingConfirmed thành công** (với user ADMIN có quyền):
   ```csharp
   [Fact]
   public async Task Confirmed_to_AccountingConfirmed_succeeds()
   {
       var draft = await CreateDraftAsync();
       await TransitionToConfirmedAsync(draft.Id);

       var result = await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);

       result.Status.Should().Be(QuotationStatus.AccountingConfirmed);
       result.AccountingConfirmedAt.Should().NotBeNull();
       result.AccountingConfirmedByUserId.Should().NotBeNull();
   }
   ```

3. **Test: AccountingConfirmed → Cancelled thành công** (ADMIN có `cancel_accounting_confirmed`):
   ```csharp
   [Fact]
   public async Task AccountingConfirmed_to_Cancelled_succeeds_with_permission()
   {
       var draft = await CreateDraftAsync();
       await TransitionToConfirmedAsync(draft.Id);
       await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);

       var result = await TransitionAsync(draft.Id, QuotationAction.Cancel);

       result.Status.Should().Be(QuotationStatus.Cancelled);
       result.CancelledAt.Should().NotBeNull();
   }
   ```

4. **Test: Draft → AccountingConfirm trả về Conflict** (transition không hợp lệ):
   ```csharp
   [Fact]
   public async Task Cannot_accounting_confirm_from_draft()
   {
       var draft = await CreateDraftAsync();
       var resp = await _client.PostAsJsonAsync(
           $"/api/quotations/{draft.Id}/transition",
           new TransitionQuotationRequest { Action = QuotationAction.AccountingConfirm });
       resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
   }
   ```

5. **Test: Sent → AccountingConfirm trả về Conflict**:
   ```csharp
   [Fact]
   public async Task Cannot_accounting_confirm_from_sent()
   {
       var draft = await CreateDraftAsync();
       await TransitionAsync(draft.Id, QuotationAction.Send);
       var resp = await _client.PostAsJsonAsync(
           $"/api/quotations/{draft.Id}/transition",
           new TransitionQuotationRequest { Action = QuotationAction.AccountingConfirm });
       resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
   }
   ```

6. **Test: Edit báo giá ở `AccountingConfirmed` trả về Conflict** (lock-at behavior):
   ```csharp
   [Fact]
   public async Task Update_on_accounting_confirmed_returns_conflict()
   {
       var draft = await CreateDraftAsync();
       await TransitionToConfirmedAsync(draft.Id);
       await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);

       var resp = await _client.PutAsJsonAsync($"/api/quotations/{draft.Id}", BuildRequest());
       // AccountingConfirmed rank=3, LockAtStatus default sẽ block hoặc service block trực tiếp.
       // Kiểm tra behavior: nếu user không có BypassLock, update bị block.
       // Lưu ý: hiện tại EnsureCanModifyAsync chỉ hard-block Cancelled.
       // AccountingConfirmed bị block qua LockAtStatus nếu user có setting đó.
       // Nếu user test chưa có LockAtStatus setting, update sẽ PASS (không bị block).
       // → Chỉ thêm test này nếu có LockAtStatus = Confirmed cho test user.
       // TODO: xác nhận test user setup trong QuotationTestBase.
   }
   ```
   **Ghi chú quan trọng**: `EnsureCanModifyAsync` hiện tại chỉ hard-block `Cancelled`. `AccountingConfirmed` chỉ bị lock nếu user có `LockAtStatus` setting. Test user trong `QuotationTestBase` cần có `LockAtStatus = Confirmed` (hoặc `AccountingConfirmed`) để test này có nghĩa. Xem xét thêm test setup hoặc bỏ test này nếu không phù hợp.

7. **Test: `AccountingConfirmedAt` timestamp hợp lệ**:
   - Đã cover trong Task 2: `result.AccountingConfirmedAt.Should().NotBeNull()`
   - Thêm: `result.AccountingConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5))`

8. **Test: Transition liên tiếp trong allowed_transitions test hiện tại** — cập nhật test `Allowed_transitions_progress_status` để include `AccountingConfirm`:
   ```csharp
   var afterAccountingConfirm = await TransitionAsync(draft.Id, QuotationAction.AccountingConfirm);
   afterAccountingConfirm.Status.Should().Be(QuotationStatus.AccountingConfirmed);
   afterAccountingConfirm.AccountingConfirmedAt.Should().NotBeNull();
   ```
   Chèn vào sau `afterConfirm` (sau khi Confirm, trước Cancel).

### Tests trong `QuotationSystemSettingsTests.cs` (file mới)

9. **Test: GET default trả về `"QuotationDate"`** (user có `system.manage_settings`):
   ```csharp
   [Fact]
   public async Task Get_returns_default_QuotationDate()
   {
       var resp = await _adminClient.GetAsync("/api/settings/quotation");
       resp.StatusCode.Should().Be(HttpStatusCode.OK);
       var dto = await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationSystemSettingsDto>>();
       dto!.Data.RevenueReportingDateField.Should().Be("QuotationDate");
   }
   ```

10. **Test: GET trả về 403 cho user không có permission** (user SALES):
    ```csharp
    [Fact]
    public async Task Get_returns_403_for_sales_user()
    {
        var resp = await _salesClient.GetAsync("/api/settings/quotation");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    ```

11. **Test: PUT với giá trị hợp lệ cập nhật và persist**:
    ```csharp
    [Fact]
    public async Task Put_valid_value_persists()
    {
        var putResp = await _adminClient.PutAsJsonAsync(
            "/api/settings/quotation",
            new { revenueReportingDateField = "AccountingConfirmedAt" });
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await _adminClient.GetAsync("/api/settings/quotation");
        var dto = await getResp.Content.ReadFromJsonAsync<ApiResponse<QuotationSystemSettingsDto>>();
        dto!.Data.RevenueReportingDateField.Should().Be("AccountingConfirmedAt");
    }
    ```

12. **Test: PUT với giá trị không hợp lệ trả về 4xx**:
    ```csharp
    [Fact]
    public async Task Put_invalid_value_returns_error()
    {
        var resp = await _adminClient.PutAsJsonAsync(
            "/api/settings/quotation",
            new { revenueReportingDateField = "InvalidField" });
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }
    ```

   Xem pattern test client setup từ `QuotationTestBase` hoặc existing integration test base class để lấy `_adminClient`, `_salesClient` đúng cách.

## Verification

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj \
  --nologo \
  --filter "QuotationStateMachine"

dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj \
  --nologo \
  --filter "QuotationSystemSettings"
```

Toàn bộ test suite:
```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo
```

## Exit Criteria

- Tất cả test mới pass
- Không có test regression trong `QuotationStateMachineTests`, `QuotationCrudTests`, `QuotationConfirmationTests`
- `AccountingConfirmedAt` được set đúng UTC timestamp khi transition
- `QuotationSystemSettingsTests`: GET default pass, PUT persist pass, 403 pass, invalid value reject pass
