# Brainstorm — Quản lý báo giá theo user, lock-status, template riêng

**Ngày**: 2026-05-14
**Phạm vi**: Báo giá (Quotation) — thêm khả năng phân quyền theo "chủ sở hữu", cấu hình ngưỡng khoá sửa theo user, và template Excel riêng cho từng user.

## Problem framing

Hiện tại mọi user có `quotations.view` đều thấy toàn bộ báo giá; không có khái niệm "owner". Trạng thái khoá sửa cứng (chỉ `Cancelled`). Template Excel dùng chung một file qua `QuotationExport:TemplatePath`. Yêu cầu:

1. Mỗi user chỉ xem báo giá thuộc về mình; Admin/Manager xem hết.
2. Một màn hình config cho phép Admin đặt ngưỡng trạng thái khoá sửa riêng cho từng user.
3. Mỗi user có thể tự upload template Excel riêng để in báo giá.

## Business context

- **Pain point**: user (sales) hiện thấy doanh thu / báo giá của đồng nghiệp — cần che để bảo mật thông tin kinh doanh giữa các sales.
- **Quy mô**: ~30 user, ~1.000 báo giá/tháng (~12k/năm). Quy mô nhỏ nên file-based template + index đơn giản trên `OwnerUserId` là đủ; không cần object storage hay sharding.
- **Stakeholder chính**: Sales Manager (đặt policy lock-at, chuyển nhượng khi nghỉ); Sales (tự quản template); Admin (cấu hình & xử lý ngoại lệ).

## Goals & non-goals

**Goals**
- Owner thực thụ trên báo giá (có thể chuyển nhượng), không chỉ dựa vào audit `CreatedBy`.
- Per-user `LockAtStatus` threshold (Draft → Sent → Confirmed → ConvertedToOrder); Admin & Manager bypass.
- Per-user template Excel (upload/replace/delete) với fallback về template hệ thống.
- **Dashboard số liệu báo giá scope theo cùng quy tắc owner** ngay từ phase này (số liệu sales chỉ là của sales đó; Admin/Manager thấy toàn bộ).
- **Cho phép clone báo giá của user đã bị soft-delete** để người kế nhiệm tiếp tục công việc.
- **Bulk-transfer** toàn bộ báo giá của 1 user sang user khác (kịch bản "user nghỉ").
- Permission code mới để có thể tái dùng cho module sau (Order, Report).

**Non-goals (phase này)**
- Không scope dữ liệu Customer / Order — giữ nguyên cho đến khi build module tương ứng.
- Không lưu chữ ký, số tài khoản, hay metadata khác trên user — chỉ template.
- Không versioning template (mỗi user 1 file, upload mới đè cũ).
- Không tự dọn file template orphan khi xoá user.
- Không thực hiện optimistic locking (ETag/RowVersion) trong phase này — chấp nhận last-write-wins.

## Constraints & assumptions

- Backend Clean Architecture .NET 9, EF Core 9 + Npgsql; pattern entity-typed.
- Auth dùng JWT + permission claim; `ICurrentUser.UserId` có sẵn.
- **Production chưa có dữ liệu** → migration NOT NULL + backfill an toàn. **Môi trường dev có dữ liệu** → migration phải có SQL backfill `OwnerUserId = COALESCE(CreatedBy, <admin-seed-id>)`, không được bỏ qua.
- Template lưu trên filesystem (đơn giản, đủ); không cần object storage trong phase này.
- "≥ trạng thái" hiểu theo thứ tự logic `Draft < Sent < Confirmed < ConvertedToOrder`. `Cancelled` (=9) vẫn cấm sửa như hiện tại, tách khỏi cơ chế lock-at.
- **Concurrent edit**: phase này chấp nhận last-write-wins. Rủi ro thấp do scope theo owner đã giới hạn số người đồng thời sửa cùng 1 báo giá (chỉ owner + admin).
- **Quy ước về lock-at sau khi transfer**: lock áp theo *user đang thao tác* (không phải user tạo gốc). Ví dụ A (lock=Sent) tạo BG, transfer cho B (lock=Confirmed), khi B sửa thì áp lock của B.

## Approaches considered

### A. Bảng `UserQuotationSettings` riêng + file template trên disk *(Chosen)*

- Entity mới `UserQuotationSettings { UserId, LockAtStatus?, TemplateFileName?, audit }`.
- Thêm `Quotation.OwnerUserId Guid NOT NULL` (FK User).
- Template lưu `templates/users/{userId}.xlsx`; fallback về `QuotationExport:TemplatePath`.

**Pros**: typed, dễ index/query, đúng pattern Clean Architecture, dễ mở rộng (chữ ký, STK sau).
**Cons**: 2 migration EF; thêm entity + EF config + service.

### B. JSON column `Preferences` trên `User` + `OwnerUserId`

- Một cột `jsonb` lưu `{ lockAtStatus, templateFile }`.

**Pros**: ít migration, ít entity.
**Cons**: phá pattern typed entity; khó validate; logic chui vào JSON parser; khó index nếu cần truy vấn.

### C. Lock-at theo role (code-config), template per-user

- `LockAtStatus` cứng theo role; chỉ template là per-user.

**Pros**: tối thiểu data.
**Cons**: trái yêu cầu rõ ràng của user (override per-user). Khi 2 sales cần threshold khác nhau là kẹt.

## Recommended approach — Phương án A

### Domain & dữ liệu

- `Quotation.OwnerUserId` NOT NULL, FK User. Migration backfill `OwnerUserId = COALESCE(CreatedBy, <admin-seed-id>)`. `CreateAsync` set từ `_currentUser.UserId` (không lấy từ request).
- Index hỗ trợ scope: `(owner_user_id, is_deleted, quotation_date DESC)`.
- Entity `UserQuotationSettings` với unique filtered index `UserId WHERE IsDeleted = false`.

### Lifecycle khi owner bị soft-delete

- Báo giá của user đã soft-delete **vẫn hiển thị** trong list của Admin/Manager (kèm badge "Owner đã nghỉ").
- Báo giá đó **không cho sửa** (kể cả admin) — chỉ cho phép 2 hành động:
  1. **Clone**: tạo bản sao mới với `OwnerUserId = currentUser`, status Draft.
  2. **Bulk-transfer**: admin chuyển toàn bộ báo giá của user nghỉ sang user khác (xem endpoint `/api/admin/users/{userId}/transfer-quotations`).
- Sales (không có `view_all`) không thấy báo giá của owner đã xoá.
- Trạng thái `Cancelled` không tính vào doanh thu trên dashboard/báo cáo (đã đúng nghiệp vụ thông thường, làm rõ ở đây để dev nhớ).

### Permissions mới (seed)

| Code | Gán mặc định | Ý nghĩa |
|---|---|---|
| `quotations.view_all` | ADMIN, MANAGER | Bypass filter `OwnerUserId` (cả list và GET đơn lẻ) |
| `quotations.transfer_own` | ADMIN, SALES, MANAGER | Chuyển báo giá *mình sở hữu* cho user khác (self-transfer khi đi công tác / bàn giao khách) |
| `quotations.transfer_any` | ADMIN | Chuyển báo giá của *bất kỳ* user nào (kể cả user đã nghỉ); gọi được endpoint bulk-transfer |
| `quotations.clone_orphan` | ADMIN, MANAGER | Clone báo giá của owner đã soft-delete |
| `quotations.bypass_lock` | ADMIN, MANAGER | Bypass lock-at threshold |
| `user_settings.manage` | ADMIN | Đặt lock-at cho user khác |

### Scoping & lock (trong `QuotationService`)

- Helper `ApplyOwnerScope(IQueryable<Quotation>)`: nếu không có `view_all` → `Where(q => q.OwnerUserId == currentUser.UserId)`.
- Helper `EnsureCanAccess(quotation)`: ném `ForbiddenException` khi không có `view_all` và `quotation.OwnerUserId != currentUser.UserId`.
- Lock check trước `UpdateAsync` / `TransitionAsync`:
  - Nếu `quotation.Owner.IsDeleted` → cấm sửa cho mọi role (chỉ clone/bulk-transfer).
  - Nếu không có `bypass_lock` và `settings.LockAtStatus is { } t` (settings của *user đang thao tác*) và `quotation.Status >= t` → `DomainException("CONFLICT", ...)`.
- DTO `QuotationDto.CanEdit` tính sẵn để FE không duplicate logic. `CanClone` cũng nên có cho trường hợp orphan.

### Audit trail

Hai loại sự kiện cần ghi log để truy vết:

| Sự kiện | Trường cần ghi | Lưu ở đâu |
|---|---|---|
| Chuyển owner báo giá | QuotationId, OldOwnerId, NewOwnerId, ActorId, Reason?, At | Bảng `QuotationOwnerHistory` (entity mới) |
| Đổi `LockAtStatus` của user | TargetUserId, OldValue, NewValue, ActorId, At | Bảng `UserSettingsHistory` (hoặc gộp 1 bảng audit chung) |
| Bulk-transfer khi user nghỉ | FromUserId, ToUserId, ActorId, AffectedCount, At | Cùng `QuotationOwnerHistory` (1 dòng/báo giá) |

Tối thiểu phase này: tạo `QuotationOwnerHistory` + log Serilog `Information` cho lock-at change (kèm `CorrelationId` đã có sẵn ở `LoggingContextMiddleware`). Có thể nâng cấp `UserSettingsHistory` sau khi nghiệp vụ audit chính thức.

### API endpoints

| Method & path | Quyền | Mô tả |
|---|---|---|
| `GET /api/me/quotation-settings` | Auth | Xem settings của user hiện tại |
| `PUT /api/me/quotation-settings/template` (multipart) | Auth | Upload/replace template |
| `DELETE /api/me/quotation-settings/template` | Auth | Xoá template (về fallback) |
| `GET /api/me/quotation-settings/template` | Auth | Download template hiện tại (preview FE) |
| `GET /api/admin/user-settings/{userId}` | `user_settings.manage` | Admin xem settings của user |
| `PUT /api/admin/user-settings/{userId}/lock-at` | `user_settings.manage` | Admin đặt `LockAtStatus`. Body: `{ "lockAtStatus": "Sent" \| "Confirmed" \| "ConvertedToOrder" \| null }` |
| `PATCH /api/quotations/{id}/owner` | `transfer_own` (nếu là chủ) hoặc `transfer_any` | Đổi `OwnerUserId`. Body: `{ "newOwnerUserId": "...", "reason"?: "..." }` |
| `POST /api/admin/users/{userId}/transfer-quotations` | `transfer_any` | Bulk: chuyển toàn bộ báo giá (không Cancelled) của user sang user khác. Body: `{ "toUserId": "...", "includeCancelled"?: false, "reason"?: "..." }`. Idempotent: an toàn gọi lại. |
| `POST /api/quotations/{id}/clone` | Auth (owner) hoặc `clone_orphan` | Tạo bản sao trạng thái Draft, `OwnerUserId = currentUser`, code mới. |
| `GET /api/dashboard/quotation-stats` | `quotations.view` | Số liệu báo giá (count, doanh thu = sum Total trừ status Cancelled). Tự scope theo owner; Admin/Manager thấy tổng. |

### Template resolution & lưu trữ

- Upload: validate MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, kích thước ≤ 5 MB (config `QuotationExport:UploadMaxBytes`).
- Đường dẫn: `templates/users/{UserId}.xlsx` (config `QuotationExport:UserTemplatesPath`).
- Render: chọn template path từ `UserQuotationSettings.TemplateFileName` của owner báo giá (chứ không phải user đang in?) — **đề xuất**: dùng template của *owner* để báo giá in ra nhất quán bất kể ai mở file. Có thể thay đổi sau.

### Frontend

- `pages/settings/my-quotation-settings.tsx`: upload/xoá template, tải về preview, hiển thị fallback notice + tên file gốc đã upload.
- `pages/admin/user-settings/[userId].tsx` (mở từ Users list): form chọn `LockAtStatus` (Không khoá / Sent / Confirmed / ConvertedToOrder).
- `pages/admin/users/{userId}/bulk-transfer.tsx` (Admin): chọn user nhận, xem trước số báo giá sẽ chuyển, confirm.
- Quotation list: thêm cột "Chủ sở hữu" khi user có `view_all`; badge "Owner đã nghỉ"; nút "Chuyển nhượng" cho owner hoặc admin; nút "Clone" cho báo giá orphan.
- Quotation form: dùng flag `canEdit` từ DTO để disable input + hiện banner khi không sửa được (lý do: khoá theo status / owner đã nghỉ / không phải chủ).
- Dashboard: hiển thị "Báo giá của tôi" cho sales, "Báo giá toàn bộ" cho Admin/Manager — cùng widget, khác data source backend.

### Kiểm thử (Integration)

- **Owner scoping**: SALES1 tạo → SALES2 list không thấy → ADMIN/MANAGER list thấy.
- **Forbidden single**: SALES2 GET báo giá của SALES1 → 403.
- **Self-transfer**: SALES1 PATCH owner BG của mình cho SALES2 → 200; SALES1 mất quyền, SALES2 có quyền.
- **Cross-user transfer denied**: SALES1 PATCH owner BG của SALES2 → 403.
- **Admin transfer-any**: ADMIN PATCH owner BG của SALES1 cho SALES2 → 200.
- **Bulk-transfer**: ADMIN POST `/users/{userId}/transfer-quotations` → N báo giá đổi owner, AffectedCount đúng, ghi N dòng audit; gọi lại idempotent (AffectedCount = 0).
- **Lock-at**: SALES có `LockAtStatus = Sent`. Q ở Draft → update OK. Q ở Sent → 400 CONFLICT. MANAGER cùng case → 200.
- **Lock after transfer**: A (Sent) tạo, transfer cho B (Confirmed). Status=Sent: A không sửa (đã mất owner), B sửa OK (lock của B chưa tới).
- **Orphan**: soft-delete user → SALES2 (`view_all` = false) không thấy báo giá; ADMIN thấy + cannot update + clone OK.
- **Cancelled**: PUT update báo giá Cancelled → 400 (giữ nguyên hành vi cũ). Dashboard sum Total bỏ qua Cancelled.
- **Template**: PUT → file tồn tại; render dùng file *của owner*; DELETE → fallback; format sai → 400.
- **Template security**: upload .xlsm → 400; upload file giả mạo MIME (text/plain rename .xlsx) → 400; zip-bomb (>50 MB sau giải nén) → 400.
- **Dashboard scope**: SALES1 GET stats → chỉ số của mình; MANAGER GET → tổng.
- **Audit log**: PATCH owner → bảng `QuotationOwnerHistory` có 1 dòng đúng `ActorId / OldOwnerId / NewOwnerId`.

### Rollout

- **3 migration EF** (theo thứ tự):
  1. `AddQuotationOwner` — thêm `OwnerUserId` (nullable trước), SQL backfill `COALESCE(CreatedBy, <admin-seed-id>)`, sau đó `ALTER ... SET NOT NULL` + index `(owner_user_id, is_deleted, quotation_date DESC)`.
  2. `AddUserQuotationSettings` — bảng settings.
  3. `AddQuotationOwnerHistory` — bảng audit transfer.
- Seed bổ sung 6 permission, map ADMIN/MANAGER/SALES tương ứng (idempotent).
- Config bổ sung trong `appsettings.json`:
  ```json
  "QuotationExport": {
    "UserTemplatesPath": "templates/users",
    "UploadMaxBytes": 5242880,
    "AllowedMimeTypes": ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
  }
  ```
- Khả thi chia 6 PR theo thứ tự: (1) entity+migration, (2) scoping+permission+owner endpoint + audit, (3) lock-at+admin/user settings, (4) template upload+render fallback+security validate, (5) bulk-transfer + clone orphan, (6) FE.
- **Communication**: trước khi deploy, gửi email/in-app banner cho sales: *"Từ ngày X bạn chỉ thấy báo giá của mình. Cần truy cập báo giá cũ → liên hệ Admin."*
- **Rollback**: 3 migration đều forward-only. Nếu cần rollback gấp, dùng feature flag `Features:QuotationOwnerScope` (boolean) — service đọc flag, false thì bypass `ApplyOwnerScope`. Flag default = true ở prod sau rollout.

## Quyết định đã chốt

| Item | Quyết định |
|---|---|
| Business pain | Bảo mật doanh thu giữa các sales (~30 user, ~1k báo giá/tháng) |
| Dashboard | Scope theo owner ngay từ phase này |
| Soft-deleted owner | Báo giá read-only, cho clone; Admin/Manager vẫn thấy |
| Cancelled | Không cộng vào doanh thu dashboard |
| Prod data | Chưa có; Dev có → migration phải có SQL backfill |
| Lock-at sau transfer | Theo user đang thao tác |
| Self-transfer | Cho phép cả Sales lẫn Admin; Admin có thêm bulk-transfer |
| Audit | Có bảng `QuotationOwnerHistory`; lock-at change log Serilog |
| Concurrent edit | Last-write-wins, không làm optimistic lock phase này |
| Acceptance Criteria | Để `write-plan` tự sinh AC dạng Given/When/Then từ test case ở SUMMARY |
| Template security | KHÔNG dùng ClamAV. Validate: MIME whitelist + magic bytes `PK\x03\x04` + parse thử bằng ClosedXML + zip-bomb cap + deny `.xlsm/.xls` |
| Reporting attribution | Doanh thu tính cho **owner hiện tại** (không phải người tạo gốc) — khớp với scope dashboard |
| OpenAPI / Swagger | Bật trong cùng plan: `AddOpenApi()` + trang `/swagger`, dev only. Mỗi controller thêm `[ProducesResponseType]` |
| Reusable scope helper | Để sau — chỉ refactor `IOwnedEntityScopeService` khi build module Order |

## Next steps

1. Sang skill `write-plan` để chia thành các phase implementation cụ thể (đã có outline 6 PR + OpenAPI task).
2. Plan sẽ tự sinh Acceptance Criteria Given/When/Then từ 13 test case ở mục "Kiểm thử (Integration)".
3. Thực thi theo plan với `execute-plan`.
