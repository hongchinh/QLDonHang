# Phase 03 — Application Layer

**Status:** [ ] pending
**Complexity:** M

## Objective

Cập nhật Application layer: DTO, validator, service để hỗ trợ state machine mới `Confirmed → AccountingConfirmed → Cancelled`.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationStatsDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Validators/UpdateLockAtRequestValidator.cs`

## Tasks

### QuotationDto.cs

1. Thêm `QuotationAction.AccountingConfirm = 3` vào enum `QuotationAction`.

2. Thêm 3 properties vào `QuotationDto` (sau `CancelledAt`):
   ```csharp
   public DateTime? AccountingConfirmedAt { get; set; }
   public Guid? AccountingConfirmedByUserId { get; set; }
   public string? AccountingConfirmedByName { get; set; }
   ```

3. Thêm 1 property vào `QuotationListItemDto` (sau `ConfirmedAt`):
   ```csharp
   public DateTime? AccountingConfirmedAt { get; set; }
   ```

### QuotationStatsDto.cs

4. Thêm 2 properties:
   ```csharp
   public int AccountingConfirmedCount { get; set; }
   public decimal AccountingConfirmedRevenue { get; set; }
   ```

### QuotationValidators.cs

5. Kiểm tra `TransitionQuotationRequestValidator` — nếu có validation `QuotationAction` enum range thì đảm bảo `AccountingConfirm = 3` được chấp nhận (thường validator chỉ check `NotEmpty`, không cần sửa, nhưng xác nhận).

### QuotationService.cs

6. **Transitions dict** — thêm 2 entries:
   ```csharp
   { (QuotationStatus.Confirmed, QuotationAction.AccountingConfirm), QuotationStatus.AccountingConfirmed },
   { (QuotationStatus.AccountingConfirmed, QuotationAction.Cancel), QuotationStatus.Cancelled },
   ```

7. **`CompareStatus`** — thêm case trong switch expression:
   ```csharp
   QuotationStatus.AccountingConfirmed => 3,
   ```
   (Đặt sau `Confirmed => 2`, trước `_ => -1`)

8. **`ApplyStatusTimestamps`** — thêm nhánh:
   ```csharp
   if (newStatus == QuotationStatus.AccountingConfirmed && q.AccountingConfirmedAt == null)
   {
       q.AccountingConfirmedAt = nowUtc;
       q.AccountingConfirmedByUserId = _currentUser.UserId;
   }
   ```

9. **`TransitionAsync`** — sau gate `CancelConfirmed`, thêm 2 gate mới:
   ```csharp
   if (action == QuotationAction.AccountingConfirm
       && !_currentUser.HasPermission(Permissions.Quotations.AccountingConfirm))
       throw new ForbiddenException("Bạn không có quyền xác nhận kế toán báo giá.");

   if (action == QuotationAction.Cancel
       && quotation.Status == QuotationStatus.AccountingConfirmed
       && !_currentUser.HasPermission(Permissions.Quotations.CancelAccountingConfirmed))
       throw new ForbiddenException("Bạn không có quyền hủy báo giá đã kế toán xác nhận.");
   ```

10. **`ActivityActionForTransition`** — thêm case:
    ```csharp
    QuotationAction.AccountingConfirm => QuotationActivityAction.AccountingConfirmed,
    ```

11. **`ActivityDescriptionForTransition`** — thêm case:
    ```csharp
    QuotationAction.AccountingConfirm => "Kế toán xác nhận đã nhận tiền",
    ```

12. **`MapToDto`** — thêm resolve `AccountingConfirmedByName` (tương tự `confirmedByName`):
    - Trong `GetAsync`: thêm đoạn query user name nếu `q.AccountingConfirmedByUserId.HasValue`
    - Trong `MapToDto` call tại `GetAsync`: truyền thêm `accountingConfirmedByName`
    - Cập nhật signature `MapToDto` để nhận thêm `string? accountingConfirmedByName = null`
    - Trong body `MapToDto`: map 3 fields mới:
      ```csharp
      AccountingConfirmedAt = q.AccountingConfirmedAt,
      AccountingConfirmedByUserId = q.AccountingConfirmedByUserId,
      AccountingConfirmedByName = accountingConfirmedByName,
      ```

13. **`QuotationListItemDto` mapping** trong `ListAsync` — thêm:
    ```csharp
    AccountingConfirmedAt = q.AccountingConfirmedAt,
    ```

14. **`EnsureCanModifyAsync`** — xem xét: hiện tại check `q.Status == QuotationStatus.Cancelled`. `AccountingConfirmed` sẽ bị khóa qua `lock-at` / `CompareStatus` logic (rank 3), không cần thêm hard-code riêng. Xác nhận logic hiện tại đã cover.

15. **`TransitionAsync` — skip `EnsureCanModifyAsync` cho `AccountingConfirm`** — tìm dòng hiện tại:
    ```csharp
    // Cancel always allowed; other actions are subject to lock-at and orphan/cancelled guards.
    if (action != QuotationAction.Cancel)
        await EnsureCanModifyAsync(quotation, ct);
    ```
    Thay bằng:
    ```csharp
    // Cancel and AccountingConfirm skip lock-at: both are transitions (not content edits),
    // each gated by its own dedicated permission.
    if (action != QuotationAction.Cancel && action != QuotationAction.AccountingConfirm)
        await EnsureCanModifyAsync(quotation, ct);
    ```
    **Lý do**: nếu ACCOUNTANT tự set `LockAtStatus = Confirmed`, `CompareStatus(Confirmed=2, Confirmed=2) >= 0` sẽ block `EnsureCanModifyAsync` khiến họ không thể transition dù có permission `quotations.accounting_confirm`. `AccountingConfirm` là state transition (không phải sửa nội dung), đã được guard bởi permission riêng — nên xử lý giống Cancel.

16. **`UpdateLockAtRequestValidator`** — thêm `AccountingConfirmed` vào `AllowedLockAt` và cập nhật error message:
    ```csharp
    private static readonly HashSet<QuotationStatus> AllowedLockAt = new()
    {
        QuotationStatus.Sent,
        QuotationStatus.Confirmed,
        QuotationStatus.AccountingConfirmed,   // ← thêm
    };
    ```
    Cập nhật message:
    ```csharp
    .WithMessage("LockAtStatus chỉ chấp nhận: null | Sent | Confirmed | AccountingConfirmed.");
    ```
    Nhờ vậy user có thể chọn `LockAtStatus = AccountingConfirmed` để lock sau khi kế toán xác nhận.

## Verification

```bash
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -nologo --verbosity minimal
```

## Exit Criteria

- Application + WebApi build thành công
- `QuotationAction.AccountingConfirm` tồn tại
- `Transitions` dict có 2 entries mới
- `CompareStatus` trả về 3 cho `AccountingConfirmed`
- `ApplyStatusTimestamps` set `AccountingConfirmedAt` và `AccountingConfirmedByUserId`
- `TransitionAsync` có 2 permission gate mới
- `TransitionAsync` skip `EnsureCanModifyAsync` cho `AccountingConfirm` action
- `MapToDto` map đủ 3 fields mới cho `QuotationDto`
- `UpdateLockAtRequestValidator` chấp nhận `AccountingConfirmed`, error message đã cập nhật
