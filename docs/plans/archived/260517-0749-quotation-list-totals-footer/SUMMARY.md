# Quotation List — Totals Footer

## Goal
Thêm footer hiển thị tổng tiền vào trang danh sách báo giá ([frontend/src/pages/quotations/quotation-list-page.tsx](../../../frontend/src/pages/quotations/quotation-list-page.tsx)). Footer luôn hiển thị (dính dưới `Card`), hiện song song 2 mức tổng — *Trang hiện tại* (FE tự sum `data.items`) và *Toàn bộ filter* (BE trả về aggregate trên toàn filter, không phân trang). Nếu bảng nhiều dòng vượt chiều cao Card thì khu vực bảng cuộn dọc, footer vẫn cố định bên dưới.

## Scope
- **In scope:**
  - Backend: thêm `QuotationListAggregates` + `QuotationListResult` (subclass `PagedResult<QuotationListItemDto>`); cập nhật `IQuotationService.ListAsync`, `QuotationService.ListAsync`, `QuotationsController.List` để trả về aggregate kèm response; thêm 6 integration test.
  - Frontend: thêm types `QuotationListAggregates`/`QuotationListResult`; đổi generic của `quotationsApi.list`; tạo component `TotalsFooter`; restructure layout `Card` của `QuotationListPage` bằng **flex chain** (`h-full` + `flex-1 min-h-0`, không hardcode pixel) để bảng cuộn trong Card và footer luôn hiện.
  - 4 cột tổng: `subtotal`, `discount`, `freight`, `total`.
  - **Cancelled handling:** mặc định **loại trừ** Cancelled khi user không lọc status (financial reporting convention). Khi user filter status có chứa Cancelled thì SUM phản ánh đúng filter (gồm cả Cancelled).
- **Out of scope:**
  - Không sửa `PagedResult<T>` chung — dùng subclass cho quotations.
  - Không sửa shared component `Table`/`Card`/`CardContent`.
  - Không thêm export tổng ra Excel/PDF.
  - Không thay đổi sort/filter hoặc business logic của báo giá.
  - Không feature flag, không migration DB.

## Assumptions
- React-query cache key `quotationKeys.list(params)` đã chứa toàn bộ filter (page, pageSize, search, statuses, customerId, from, to) → khi đổi page sẽ refetch và `aggregates` được tính lại đúng cùng filter. (Chấp nhận refetch nhỏ — đánh đổi để giữ 1 round-trip và dữ liệu nhất quán.)
- Shell layout ([`app-layout.tsx:126,142`](../../../frontend/src/components/layout/app-layout.tsx#L126-L142)) đã có `<main className="flex flex-1 flex-col overflow-hidden">` và content `<div className="flex-1 overflow-y-auto p-4 md:p-3">`. Page wrapper dùng `h-full flex flex-col min-h-0` thừa kế chiều cao này → Card có thể `flex-1 min-h-0`, vùng table có `overflow-y-auto` mà không xung đột với shell scroll (shell scroll vẫn fallback khi page content vượt — khi page = `h-full` thì shell scroll không kích hoạt).
- Các integration test hiện tại deserialize bằng `PagedResult<QuotationListItemDto>` vẫn hoạt động vì JSON deserialization bỏ qua field thừa `aggregates` (no breaking change cho test cũ).
- Sum decimal trên PostgreSQL với 4 trường `Subtotal/Discount/Freight/Total` chạy nhanh (đã có index trên các trường filter); không cần index mới. Phase 01 sẽ log SQL output 1 lần để xác nhận query plan hợp lý (không ORDER BY thừa, dùng index của filter).

## Risks
- **Layout regression:** đổi `Card` flat → flex-column chain có thể ảnh hưởng các trang khác nếu vô tình sửa component dùng chung — chỉ sửa **inline className** trong `quotation-list-page.tsx`, không sửa `Card`/`CardContent`/`Table` chung.
- **Double scroll với shell:** shell content có `overflow-y-auto`. Nếu chain `h-full + flex-1 min-h-0` sai khâu (thiếu `min-h-0` ở bước nào đó), Card có thể vượt chiều cao → shell tự cuộn → user thấy 2 scroll. Mitigation: smoke test có case >25 báo giá; nếu thấy double scroll, debug bằng cách thêm `min-h-0` ở khâu thiếu.
- **Test cũ vỡ:** dù JSON deserialization bỏ qua extra field, nếu strict mode được bật ở `TestJson.Options` có thể fail → phase 1 verify chạy lại các test cũ ngay sau khi đổi DTO.
- **Owner-scope leak:** aggregate phải chạy **sau** `ApplyOwnerScope` để không lộ tổng tiền của user khác cho user không có `quotations.view_all`. `ApplyOwnerScope` đã được apply ở đầu `query` ([QuotationService.cs:125](../../../backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs#L125)) → aggregate dùng cùng `query` sẽ tự động owner-scoped. Test case `aggregates_respect_owner_scope` verify hành vi này.
- **Footer KHÔNG align với cột bảng:** footer dùng grid 2×5 độc lập (không phải `<TableFooter>` bên trong `<Table>`). Trade-off vì `<TableFooter>` sticky inside scrolling container đòi hỏi modify shared `Table` component (out of scope). Mitigation: footer cells dùng `text-right tabular-nums` để cảm giác cột số; label "Trang hiện tại"/"Toàn bộ filter" ở cột đầu rộng cố định 140px.
- **Cancelled toggle logic dễ misread:** rule "exclude khi không filter status, include khi filter explicit chứa Cancelled" — viết clear trong code comment ngắn (1 dòng) + 2 test case `aggregates_exclude_cancelled_by_default` và `aggregates_include_cancelled_when_explicitly_filtered`.

## Phases
- [ ] Phase 01 — Backend aggregates trong list response (M) — [phase-01-backend-aggregates.md](phase-01-backend-aggregates.md)
- [ ] Phase 02 — Frontend totals footer + cuộn dọc trong Card (M) — [phase-02-frontend-totals-footer.md](phase-02-frontend-totals-footer.md)

## Final Verification
Sau khi cả 2 phase pass:

1. **Backend build & test** (chỉ Application + WebApi + IntegrationTests, không restart WebApi đang chạy):
   ```powershell
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
   dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --filter "FullyQualifiedName~QuotationListFilterTests"
   ```
2. **Frontend type-check & lint:**
   ```powershell
   npm --prefix frontend run build
   npm --prefix frontend run lint
   ```
3. **Manual UI smoke test** trên `/quotations`:
   - Footer hiện 2 dòng (`Trang hiện tại`, `Toàn bộ filter`), mỗi dòng 4 ô tiền; cột "Tổng tiền" in đậm.
   - Đổi page → "Trang hiện tại" cập nhật; "Toàn bộ filter" giữ nguyên (vì BE tính trên toàn filter, không phân trang).
   - Đổi filter (status / from / to / search) → cả 2 dòng cập nhật.
   - Tạo (hoặc seed) >25 báo giá để bảng vượt chiều cao Card → khu vực bảng cuộn dọc **bên trong** Card, không kích hoạt shell scroll; footer + pagination vẫn nhìn thấy ở đáy Card. Filter bar không cuộn theo.
   - Bảng đang loading → 8 ô là skeleton bar (không phải text `—`); cấu trúc footer vẫn hiện.
   - Bảng rỗng (filter không match) → 8 ô = `0` (rõ ràng khác trạng thái loading).
   - Network error → 8 ô là `—`; banner lỗi vẫn hoạt động.
   - User không có `quotations.view_all`: "Toàn bộ filter" chỉ phản ánh tổng của báo giá mình sở hữu (không lộ tổng global).
   - **Cancelled handling:**
     - Không filter status → "Toàn bộ filter" KHÔNG tính báo giá Cancelled (so sánh DB sum của Draft+Sent+Confirmed).
     - Filter status = `Cancelled` → "Toàn bộ filter" CHỈ tính báo giá Cancelled.
     - Filter status = `Cancelled,Draft` → "Toàn bộ filter" tính cả 2.

## Rollback / Recovery
- Không có migration DB → revert là an toàn bằng `git revert <commit>`.
- Nếu chỉ phase 02 lỗi UI: revert phase 02 commit; phase 01 (BE trả thêm `aggregates`) backward-compatible (FE cũ bỏ qua field thừa).
- Nếu phase 01 gây regression test: revert đổi return type về `PagedResult<QuotationListItemDto>` ở `IQuotationService` + `QuotationsController`; xóa class `QuotationListAggregates`/`QuotationListResult`.
