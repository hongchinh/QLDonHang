# Gửi File Báo Giá vào Nhóm Zalo qua Zalo OA

**Date:** 2026-05-22  
**Status:** Design approved, ready for implementation

---

## Problem Framing

Hiện tại, hệ thống có thể xuất báo giá thành file Excel hoặc PDF, nhưng nhân viên phải tải file về rồi gửi tay vào nhóm Zalo của khách hàng. Mục tiêu là tự động hóa bước này bằng một nút "Gửi Zalo" trên giao diện báo giá.

---

## Goals & Non-Goals

**Goals:**
- Thêm nút "Gửi Zalo" trên trang báo giá
- Cho phép user chọn gửi Excel hoặc PDF
- Gửi file vào nhóm Zalo tương ứng với từng khách hàng
- Lưu `ZaloGroupId` theo từng khách hàng

**Non-Goals:**
- Gửi tin nhắn text (chỉ gửi file)
- Tự động refresh Zalo access token (scope sau nếu cần)
- Lịch sử gửi Zalo (không track trong DB)
- Gửi vào nhiều nhóm cùng lúc

---

## Constraints & Assumptions

- Zalo OA đã được tạo; cần lấy access token từ Zalo Developer Portal
- OA phải được thêm thủ công vào từng nhóm Zalo trước khi gửi
- Access token có thời hạn ~90 ngày, cần cập nhật thủ công khi hết hạn
- LibreOffice đã cài để convert PDF (nếu chọn gửi PDF)
- Mỗi khách hàng có một `ZaloGroupId` duy nhất (nullable)

---

## Approaches Considered

### Option 1: Inline trong Controller (Rejected)
Gọi Zalo API trực tiếp trong `QuotationsController`. Nhanh nhưng vi phạm clean architecture, khó test.

### Option 2: Clean Service Layer (Chosen)
Tạo `IZaloOaService` interface trong Application layer, `ZaloOaService` implementation trong Infrastructure. Phù hợp với kiến trúc hiện có, dễ mock trong test, dễ swap provider.

### Option 3: Background Job (Rejected)
Dùng Hangfire/Worker cho gửi async. Overkill cho file nhỏ, yêu cầu thêm infrastructure.

---

## Recommended Approach: Clean Service Layer

### Backend Changes

#### 1. Domain — `Customer.cs`
Thêm field:
```csharp
public string? ZaloGroupId { get; set; }
```

#### 2. Application — `IZaloOaService.cs`
```csharp
public interface IZaloOaService
{
    Task<string> UploadFileAsync(byte[] fileData, string fileName, CancellationToken ct = default);
    Task SendFileToGroupAsync(string groupId, string fileToken, CancellationToken ct = default);
}
```

#### 3. Infrastructure — `ZaloOaService.cs`
Implement với `HttpClient`:

**Bước 1 — Upload file:**
```
POST https://upload.zaloapp.com/v2/oa/upload/file
Header: access_token: <token>
Body: multipart/form-data { file: <bytes> }
Response: { error: 0, data: { token: "abc123" } }
```

**Bước 2 — Gửi vào group:**
```
POST https://openapi.zalo.me/v3.0/oa/group/sendmessage
Header: access_token: <token>
Body: {
  "recipient": { "group_id": "<ZaloGroupId>" },
  "message": {
    "attachments": [{
      "type": "file",
      "payload": { "token": "abc123" }
    }]
  }
}
```

#### 4. Infrastructure — `ZaloOaOptions.cs`
```csharp
public class ZaloOaOptions
{
    public string AccessToken { get; set; } = "";
    public string OaId { get; set; } = "";
}
```

Cấu hình trong `appsettings.json`:
```json
"ZaloOA": {
  "AccessToken": "<lấy từ Zalo Developer Portal>",
  "OaId": "<OA ID>"
}
```

#### 5. Application — `SendQuotationToZaloRequest.cs`
```csharp
public record SendQuotationToZaloRequest(string FileType); // "excel" | "pdf"
```

#### 6. Application — `IQuotationService` extension
```csharp
Task SendToZaloAsync(Guid quotationId, string fileType, CancellationToken ct = default);
```

Logic:
1. Load Quotation + Customer (kiểm tra `ZaloGroupId` không null)
2. Generate file (tái dùng `IQuotationExcelRenderer` và `IQuotationSpreadsheetPdfConverter`)
3. Upload file → lấy token
4. Gửi text message vào group: `"Báo giá {Code} ngày {QuotationDate:dd/MM/yyyy}"`
5. Gửi file (token) vào group

#### 7. WebApi — `QuotationsController.cs`
```
POST /quotations/{id}/send-zalo
Body: { "fileType": "excel" | "pdf" }
Response 200: { "message": "Đã gửi file vào nhóm Zalo" }
Response 400: { "error": "Khách hàng chưa có Zalo Group ID" }
Response 502: { "error": "Zalo API error: ..." }
```

#### 8. Migration
```
AddColumn: Customers.ZaloGroupId (nvarchar, nullable)
```

---

### Frontend Changes

#### Customer form
Thêm field "Zalo Group ID" (optional) trong form tạo/sửa khách hàng:
```
Label: Zalo Group ID
Type: text input (optional)
Tooltip: "ID nhóm Zalo để nhận báo giá tự động"
```

#### Quotation form/detail — Nút "Gửi Zalo"
Thêm nút `📤 Gửi Zalo` cạnh các nút download hiện có.

#### Component `SendZaloDialog.tsx`
Dialog khi bấm "Gửi Zalo":
```
Tiêu đề: Gửi báo giá vào nhóm Zalo
Nội dung:
  - Radio: ○ Excel (.xlsx)  ○ PDF (.pdf)
  - Hiển thị: "Gửi đến: [ZaloGroupId của khách hàng]"
  - Cảnh báo nếu ZaloGroupId trống
Buttons: [Hủy] [Gửi ngay]
```

**Files thay đổi:**

| File | Thay đổi |
|------|----------|
| `features/quotations/api.ts` | Thêm `sendToZalo(id, fileType)` |
| `features/customers/api.ts` | Thêm `zaloGroupId` vào types và calls |
| `pages/quotations/quotation-form-page.tsx` | Thêm nút + `SendZaloDialog` |
| `pages/customers/customer-form.tsx` | Thêm field ZaloGroupId |
| `components/SendZaloDialog.tsx` | New component |

---

## Token Management (Proactive Refresh)

### Cơ chế Zalo Token

| Token | Thời hạn điển hình |
|-------|-------------------|
| `access_token` | ~90 ngày |
| `refresh_token` | ~3 tháng |

Khi cả hai hết hạn → admin paste token mới từ Developer Portal qua endpoint.

### DB Entity: `ZaloOaToken`

```csharp
public class ZaloOaToken : BaseEntity
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public DateTime LastRefreshedAt { get; set; }
}
```
Bảng singleton (1 row duy nhất).

### `IZaloTokenManager` — Proactive Refresh Logic

```
GetValidAccessTokenAsync():
  ├─ AccessToken còn hạn > 24h → trả về ngay
  ├─ Sắp hết hạn (<24h) → gọi Zalo refresh API → lưu tokens mới vào DB
  └─ RefreshToken hết hạn → throw ZaloTokenExpiredException
```

Refresh endpoint Zalo:
```
POST https://oauth.zaloapp.com/v4/oa/access_token
Body: { "app_id": "...", "grant_type": "refresh_token", "refresh_token": "..." }
Response: { "access_token": "...", "refresh_token": "...", "expires_in": 7776000 }
```

### Admin Endpoint (cập nhật token thủ công khi cả hai hết hạn)

```
PUT /api/settings/zalo-token
Body: { "accessToken": "...", "refreshToken": "...", "accessTokenExpiresAt": "ISO8601" }
Permission: admin only
```

### appsettings.json (chỉ config tĩnh — không lưu token)

```json
"ZaloOA": {
  "AppId": "<app_id>",
  "AppSecret": "<app_secret>",
  "UploadUrl": "https://upload.zaloapp.com/v2/oa/upload/file",
  "MessageUrl": "https://openapi.zalo.me/v3.0/oa/group/sendmessage",
  "TokenRefreshUrl": "https://oauth.zaloapp.com/v4/oa/access_token"
}
```

### Files bổ sung cho Token Management

| Layer | File | Mục đích |
|-------|------|----------|
| Domain | `ZaloOaToken.cs` | Entity lưu token + expiry |
| Application | `IZaloTokenManager.cs` | Interface: `GetValidAccessTokenAsync()` |
| Infrastructure | `ZaloTokenManager.cs` | Proactive refresh implementation |
| WebApi | `SettingsController.cs` | `PUT /api/settings/zalo-token` |
| Migration | `..._AddZaloOaTokens` | Tạo bảng ZaloOaTokens |

---

## Setup Zalo OA Token (Manual — Before Implementation)

1. Đăng nhập [developers.zalo.me](https://developers.zalo.me) với tài khoản quản trị OA
2. Tạo ứng dụng → chọn "Zalo Official Account API"
3. Kết nối OA với ứng dụng → lấy `OA ID`
4. Cấp quyền: `send_message`, `manage_official_account`
5. Tab "Xác thực" → tạo Access Token → copy vào `appsettings.json`
6. Thêm OA vào từng nhóm Zalo của khách hàng (thủ công từ Zalo mobile)
7. Lấy Group ID (qua Zalo API hoặc dashboard)

> **Cảnh báo:** Access token hết hạn sau ~90 ngày. Khi backend trả lỗi 502 liên quan Zalo, kiểm tra token trước.

---

## Open Questions

1. Zalo OA cần được thêm vào nhóm bằng cách nào — admin thêm thủ công hay có flow tự động?
2. Group ID lấy từ đâu nếu admin chưa biết — cần tool hỗ trợ hay hướng dẫn thủ công?
3. ~~Có cần message kèm file?~~ → **Đã quyết định:** Gửi text `"Báo giá {Code} ngày {QuotationDate:dd/MM/yyyy}"` trước, sau đó attach file.

---

## Next Steps

1. Setup Zalo OA token (thủ công — xem hướng dẫn trên)
2. Tạo implementation plan chi tiết theo phases
3. Phase 1: Backend — migration + `ZaloOaService` + endpoint
4. Phase 2: Frontend — Customer form field + `SendZaloDialog` + nút trên quotation
5. Phase 3: Test end-to-end với OA thật
