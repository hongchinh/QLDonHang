# Phase 01 — Backend DTO + Service + Validator

**Status:** [x] completed
**Complexity:** M

## Objective
Mở rộng `QuotationListItemDto` với 3 field `Subtotal/Discount/Freight` và chuyển `QuotationListRequest.Status` thành dạng comma-separated string để hỗ trợ multi-status filter.

## Files
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Helpers/QuotationStatusListParser.cs` (mới)

## Tasks

### 1. Mở rộng `QuotationListItemDto`
File: `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` (sau line 96 `public decimal Total`).

Thêm 3 property:
```csharp
public decimal Subtotal { get; set; }
public decimal Discount { get; set; }
public decimal Freight { get; set; }
```
Vị trí: ngay trước `public decimal Total { get; set; }` để giữ thứ tự logic (Subtotal → Discount → Freight → Total).

### 2. Đổi `QuotationListRequest.Status` thành multi-value
File: cùng file, line 150-156. Thay:
```csharp
public QuotationStatus? Status { get; set; }
```
bằng:
```csharp
// Comma-separated list of QuotationStatus values, e.g. "Draft,Sent".
// Backward compatible: single value "Draft" vẫn hoạt động (split cho ra list 1 phần tử).
public string? Status { get; set; }
```

### 3. Tạo helper parse dùng chung (single source of truth)
File mới: `backend/src/OrderMgmt.Application/Sales/Quotations/Helpers/QuotationStatusListParser.cs`

```csharp
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Sales.Quotations.Helpers;

internal static class QuotationStatusListParser
{
    /// <summary>
    /// Parse comma-separated status string into a list of valid enum values.
    /// Invalid tokens are silently dropped — validator chịu trách nhiệm reject 400 trước khi tới service.
    /// </summary>
    public static IReadOnlyList<QuotationStatus> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<QuotationStatus>();
        var result = new List<QuotationStatus>();
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<QuotationStatus>(token, ignoreCase: true, out var s))
                result.Add(s);
        }
        return result;
    }

    /// <summary>True nếu tất cả token đều parse được thành enum hợp lệ. Empty/null → true (no-filter).</summary>
    public static bool IsValid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return true;
        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<QuotationStatus>(token, ignoreCase: true, out _))
                return false;
        }
        return true;
    }
}
```

**Lý do tách helper:** validator và service đều cần parse-logic giống hệt. Tránh drift khi enum thay đổi (đặc biệt sau pivot Quotation-only — xem [[project_quotation_only_pivot]]).

### 4. Cập nhật `QuotationService.ListAsync`
File: `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`.

Thêm `using OrderMgmt.Application.Sales.Quotations.Helpers;` ở đầu file (nếu chưa có).

Tại line 128-129, thay:
```csharp
if (request.Status.HasValue)
    query = query.Where(q => q.Status == request.Status.Value);
```
bằng:
```csharp
var statuses = QuotationStatusListParser.Parse(request.Status);
if (statuses.Count > 0)
    query = query.Where(q => statuses.Contains(q.Status));
```

### 5. Cập nhật projection
File: cùng file, line 161-191 — thêm 3 field vào projection `Select(q => new QuotationListItemDto { ... })`:
```csharp
Subtotal = q.Subtotal,
Discount = q.Discount,
Freight = q.Freight,
```
Vị trí: trước dòng `Total = q.Total,`.

### 6. Cập nhật validator (reuse helper)
File: `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs`, line 54-56.

Thêm ở đầu file:
```csharp
using OrderMgmt.Application.Sales.Quotations.Helpers;
```

Thay:
```csharp
public class QuotationListRequestValidator : PageRequestValidator<QuotationListRequest>
{
}
```
bằng:
```csharp
public class QuotationListRequestValidator : PageRequestValidator<QuotationListRequest>
{
    public QuotationListRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(QuotationStatusListParser.IsValid)
            .WithMessage("Trạng thái không hợp lệ. Giá trị cho phép: Draft, Sent, Confirmed, Cancelled.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }
}
```

Validator giờ chỉ delegate parsing — single source of truth nằm ở helper.

## Verification

```powershell
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~QuotationCrud"
```

Tests `QuotationCrudTests` không assert trực tiếp lên `QuotationListItemDto` fields mới, nên should pass. Nếu có test list cụ thể assert Total mà không có 3 field mới, vẫn pass vì chỉ thêm field, không xóa.

## Exit Criteria
- 2 project compile sạch (0 error, 0 warning từ code thay đổi).
- Test `QuotationCrudTests` pass full.
- API `GET /api/quotations?status=Draft,Sent` không trả 400 và filter đúng (kiểm thông qua Phase 04).
- API `GET /api/quotations?status=Draft` (legacy) vẫn hoạt động.
- API `GET /api/quotations?status=Invalid` trả 400 với message validate.
