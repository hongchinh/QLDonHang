# Phase 01 — Backend: DTO, parser, validator, service filter

**Status:** [ ] pending
**Complexity:** M

## Objective
Mở rộng `QuotationListRequest` với `OwnerUserIds` (CSV Guid), thêm helper `OwnerIdListParser` theo pattern `QuotationStatusListParser`, validate, và áp filter trong `QuotationService.ListAsync` — chỉ honored khi caller có `quotations.view_all`.

## Files
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` (sửa class `QuotationListRequest`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Helpers/OwnerIdListParser.cs` (mới)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs` (sửa `QuotationListRequestValidator`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (sửa `ListAsync`)

## Tasks
1. **Tạo helper** `OwnerIdListParser.cs` trong `Helpers/`:
   ```csharp
   namespace OrderMgmt.Application.Sales.Quotations.Helpers;

   internal static class OwnerIdListParser
   {
       public static IReadOnlyList<Guid> Parse(string? raw)
       {
           if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<Guid>();
           var result = new List<Guid>();
           foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
               if (Guid.TryParse(token, out var g)) result.Add(g);
           return result;
       }

       public static bool IsValid(string? raw)
       {
           if (string.IsNullOrWhiteSpace(raw)) return true;
           foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
               if (!Guid.TryParse(token, out _)) return false;
           return true;
       }
   }
   ```

2. **Sửa `QuotationListRequest`** trong `QuotationDto.cs`:
   ```csharp
   public class QuotationListRequest : PageRequest
   {
       public string? Status { get; set; }
       public Guid? CustomerId { get; set; }
       public DateOnly? From { get; set; }
       public DateOnly? To { get; set; }
       // NEW:
       public string? OwnerUserIds { get; set; }   // CSV "guid1,guid2"; honored only when caller has quotations.view_all
   }
   ```

3. **Sửa `QuotationListRequestValidator`** trong `QuotationValidators.cs`:
   ```csharp
   public QuotationListRequestValidator()
   {
       RuleFor(x => x.Status)
           .Must(QuotationStatusListParser.IsValid)
           .WithMessage("Trạng thái không hợp lệ. Giá trị cho phép: Draft, Sent, Confirmed, Cancelled.")
           .When(x => !string.IsNullOrWhiteSpace(x.Status));

       RuleFor(x => x.OwnerUserIds)
           .Must(OwnerIdListParser.IsValid)
           .WithMessage("Danh sách chủ sở hữu chứa giá trị không phải Guid hợp lệ.")
           .When(x => !string.IsNullOrWhiteSpace(x.OwnerUserIds));
   }
   ```

4. **Sửa `QuotationService.ListAsync`** ([QuotationService.cs:123-145](../../backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs#L123-L145)) — thêm filter clause SAU `ApplyOwnerScope` và các filter status/customer/date hiện có, TRƯỚC tính aggregate:
   ```csharp
   if (_currentUser.HasPermission(Permissions.Quotations.ViewAll)
       && !string.IsNullOrWhiteSpace(request.OwnerUserIds))
   {
       var ownerIds = OwnerIdListParser.Parse(request.OwnerUserIds);
       if (ownerIds.Count > 0)
           query = query.Where(q => ownerIds.Contains(q.OwnerUserId));
   }
   ```
   Đặt block này ngay SAU filter `request.To.HasValue` và TRƯỚC block `request.Search`. Vị trí cụ thể: dòng ~138 (sau date filter, trước search).

   **Lý do guard `HasPermission` ở đây** (không chỉ dựa vào FE): nếu một sale forge URL với `?ownerUserIds=...`, request đến BE thì `ApplyOwnerScope` đã trim về `OwnerUserId == self`, nhưng filter `Contains(ownerIds)` sẽ thu hẹp tiếp về Ø → sale thấy danh sách rỗng (UX confusing) hoặc thấy 0 báo giá khi đáng ra có. Silently ignore trả về behavior an toàn.

## Verification
```powershell
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
```
- Build phải xanh, không warning mới.
- Grep verify: `OwnerIdListParser` được reference đúng 2 chỗ (service + validator).

## Exit Criteria
- [ ] `OwnerIdListParser.cs` tồn tại, methods `Parse` và `IsValid` đúng signature.
- [ ] `QuotationListRequest.OwnerUserIds` xuất hiện.
- [ ] `QuotationListRequestValidator` có rule mới cho `OwnerUserIds`.
- [ ] `QuotationService.ListAsync` có block filter mới, đặt sau date filter và trước search filter.
- [ ] Build Application project thành công.
- [ ] Không sửa bất kỳ file nào ngoài 4 file liệt kê.
