# Brainstorm — Customer Autocomplete cho Form Báo giá

> Ngày: 2026-05-13 · Topic: Thay block "Thông tin khách hàng" trong [frontend/src/pages/quotations/quotation-form-page.tsx](../../../frontend/src/pages/quotations/quotation-form-page.tsx) bằng autocomplete theo chuẩn [docs/bd/phan-tich-hanh-vi-4-2-ma-doi-tuong.md](../../bd/phan-tich-hanh-vi-4-2-ma-doi-tuong.md).

---

## 1. Problem framing

Form Báo giá hiện tại dùng `<Select>` đơn giản load sẵn 200 khách hàng ([quotation-form-page.tsx:52, 217-232](../../../frontend/src/pages/quotations/quotation-form-page.tsx)). Khi danh sách khách hàng lớn, UX kém: phải scroll, không tìm được theo MST/SĐT/địa chỉ, không hỗ trợ thêm nhanh khi chưa có KH trong danh mục. BD §4.2 mô tả chuẩn autocomplete nghiệp vụ keyboard-first, phù hợp cho người nhập liệu tốc độ cao — phù hợp áp dụng cho block "Thông tin khách hàng" của báo giá (sau khi tinh chỉnh cho khác biệt nghiệp vụ).

---

## 2. Goals & Non-goals

### Goals
1. Thay block "Thông tin khách hàng": dùng autocomplete (search-as-you-type) thay Select.
2. Bổ sung trường **Tên khách hàng** snapshot (cho phép sửa tay) — persist xuống backend, dùng khi in PDF.
3. Auto-fill block giao hàng (`deliveryAddress`, `deliveryRecipient`, `deliveryPhone`) từ thông tin KH — chỉ khi field đang trống, không ghi đè dữ liệu user.
4. Quick-add khách hàng qua Dialog inline (không mất form báo giá đang nhập).
5. Endpoint mới `GET /api/customers/search` tối ưu cho typeahead (Code/Name/TaxCode/CompanyAddress/PhoneNumber, activeOnly mặc định).
6. Keyboard navigation đầy đủ theo BD §7 (bao gồm Tab cycle trong dropdown).

### Non-goals
- Không thay đổi luồng status/transitions của báo giá.
- Không thêm "Lý do nộp" (không thuộc nghiệp vụ báo giá).
- Không refactor LineItemsGrid hay TotalsPanel.
- Không disable customer khi báo giá != Draft (giữ hành vi hiện tại — luôn cho đổi).
- Không dùng `customerName` snapshot cho search/filter trong danh sách báo giá (chỉ dùng trên PDF in).

---

## 3. Constraints & Assumptions

- Customer entity hiện có: `code`, `name`, `taxCode`, `companyAddress`, `defaultShippingAddress`, `contactPerson`, `phoneNumber`, `email`, `group`, `status` ([frontend/src/features/customers/types.ts](../../../frontend/src/features/customers/types.ts)).
- API `/customers` hiện có filter `search` + `status` nhưng chưa tối ưu cho typeahead (không rõ search trên những cột nào, có hỗ trợ không dấu hay không).
- Form báo giá dùng `react-hook-form` + `zodResolver` + `react-router-dom`.
- BD đặc biệt: Tab khi dropdown mở duyệt row + vòng lại (giữ đúng BD spec dù khác convention web).
- Báo giá lines độc lập với KH → khi đổi KH không cần update grid (khác BD nói "bảng hạch toán không update").

---

## 4. Decisions (user-approved)

| Câu hỏi | Lựa chọn |
|---|---|
| Trường "Tên KH snapshot" | **Thêm field `customerName` + persist backend** |
| Auto-fill delivery* | **Có, chỉ khi field đang trống** |
| Icon "+" thêm KH | **Mở Dialog/Modal inline** |
| Search backend | **Tạo endpoint mới `/customers/search`** |
| Dùng `customerName` | **Hiển thị trên PDF in báo giá** |
| Tab trong dropdown | **Giữ đúng BD: duyệt row + vòng lại** |
| Disable khi status != Draft | **Không, luôn cho đổi** |
| Quick-add Dialog | **Full form (giống customer-form-page)** |

---

## 5. Approaches considered

### A. Autocomplete component dùng chung + endpoint search mới (CHỌN)
Tạo `CustomerAutocomplete` component + endpoint `/customers/search` riêng. Có thể tái sử dụng cho các form khác (đơn hàng, phiếu thu sau này).
- **Pros**: Tách biệt rõ, performance tốt (typeahead riêng), test độc lập, mở rộng dễ.
- **Cons**: Thêm 1 endpoint backend, cần refactor `customer-form-page` để chia sẻ form fields.
- **Complexity**: Medium.

### B. Mở rộng `/customers` cho typeahead
Thêm tham số `limit`, `format=compact` vào endpoint cũ.
- **Pros**: Không thêm endpoint.
- **Cons**: Endpoint list bị quá tải nhiều mode, response shape không nhất quán.

### C. Endpoint chung `/objects/search?objectTypes=Customer` theo BD
Như BD đề xuất, cho dùng chung với Phiếu thu sau này.
- **Pros**: Đúng BD spec, mở rộng được nhiều loại đối tượng.
- **Cons**: Báo giá chỉ cần Customer → over-engineering ở phase này.

---

## 6. Recommended approach

**Approach A**. Đáp ứng đủ AC §16 BD, tối ưu cho báo giá, refactor tối thiểu. Có thể nâng cấp thành Approach C sau khi triển khai cho Phiếu thu nếu cần.

### Kiến trúc (luồng)
```
[CustomerAutocomplete] ── debounced search ──> GET /customers/search
       │
       ├── chọn KH ──> setValue(customerId, customerName)
       │                ├── fill customerName (cho sửa)
       │                └── auto-fill delivery* (chỉ khi trống)
       │
       └── click "+" ──> <CustomerQuickAddDialog>
                             │
                             └── onCreated(c) ──> auto-select c, focus → "Tên KH"
```

### Components/files mới
- `frontend/src/pages/quotations/components/customer-autocomplete.tsx`
- `frontend/src/pages/quotations/components/customer-quick-add-dialog.tsx`
- Tách `frontend/src/pages/customers/customer-form-page.tsx` → component `CustomerFormFields` dùng chung.
- Hook `useCustomersSearch(keyword)` trong `features/customers/hooks.ts`.

### Thay đổi backend
- Migration: thêm cột `CustomerNameSnapshot NVARCHAR(255) NULL` vào `Quotations`.
- Endpoint `GET /api/customers/search?keyword=&activeOnly=true&limit=20`.
- Update DTO/Request/Response của Quotation, mapper, PDF generator (`displayName = CustomerNameSnapshot ?? Customer.Name`).

### AC mapping
Toàn bộ 33 AC trong BD §16 đều cover được, ngoại trừ:
- **AC-OBJ-027** (default object type = Customer): không áp dụng — báo giá chỉ có Customer.
- **AC-OBJ-029** (đổi KH không update bảng hạch toán): áp dụng tự nhiên — báo giá lines độc lập với KH.

---

## 7. Technical details

### 7.1 Schema (frontend)
```ts
// quotations/schema.ts — thêm field
customerId: z.string().uuid('Chọn khách hàng'),
customerName: optionalString(255),
```

### 7.2 Types
```ts
// quotations/types.ts — thêm vào Quotation + UpsertQuotationRequest
customerName?: string;

// customers/types.ts — type mới
type CustomerSearchItem = {
  id: string; code: string; name: string;
  taxCode?: string; companyAddress?: string;
  defaultShippingAddress?: string; contactPerson?: string;
  phoneNumber?: string; status: 'Active' | 'Inactive';
};
```

### 7.3 API
```http
GET /api/customers/search?keyword=abc&activeOnly=true&limit=20
```
- Backend: search ILIKE/CONTAINS trên `Code | Name | TaxCode | CompanyAddress | PhoneNumber`.
- Vietnamese không dấu: dùng cột `NameNoDiacritic` (nếu chưa có thì thêm computed/trigger column).
- `activeOnly=true` mặc định.
- `limit` clamp [1, 50], mặc định 20.

### 7.4 Component state (BD §17, rút gọn)
```ts
{
  keyword: string;
  selectedCustomer: CustomerSearchItem | null;
  results: CustomerSearchItem[];
  isOpen: boolean;
  isLoading: boolean;
  highlightedIndex: number;
  error?: string;
  isDirty: boolean;
}
```

### 7.5 Keyboard map (BD §18)
| Phím | Hành vi |
|---|---|
| ArrowDown / ArrowUp | Move highlight |
| Tab / Shift+Tab | Move highlight, wrap (không thoát field) |
| Enter | Chọn highlight; nếu chỉ 1 result và keyword khớp đúng `code` → auto-chọn |
| Esc | Đóng dropdown, giữ keyword |
| Click row | Chọn |

### 7.6 Khi chọn KH — handler
```
form.setValue('customerId', c.id);
form.setValue('customerName', c.name, { shouldDirty: true });
if (!form.getValues('deliveryAddress'))
  form.setValue('deliveryAddress', c.defaultShippingAddress ?? c.companyAddress ?? '');
if (!form.getValues('deliveryRecipient'))
  form.setValue('deliveryRecipient', c.contactPerson ?? '');
if (!form.getValues('deliveryPhone'))
  form.setValue('deliveryPhone', c.phoneNumber ?? '');
closeDropdown();
focus('input[name="customerName"]');
```

### 7.7 Edge cases
| Tình huống | Xử lý |
|---|---|
| User gõ text không chọn | Submit → zod báo `'Chọn khách hàng'`, focus về autocomplete |
| Đổi KH sau khi có lines | Không đụng lines; chỉ update header + auto-fill delivery (chỉ khi trống) |
| Xóa customerId trên báo giá đã save | Cho clear local; submit fails validation cho đến khi chọn lại |
| API error | Hiển thị message trong dropdown, giữ keyword |
| KH inactive đã chọn từ trước (edit báo giá cũ) | Hiển thị tên + badge "Ngừng sử dụng"; search ẩn inactive nhưng giữ giá trị đã chọn |
| `customerName` rỗng khi submit | Backend fallback = `Customer.Name`; FE blur revert về master name nếu user clear hẳn |

---

## 8. Delivery (6 bước)

1. **Backend foundation** — migration + entity + DTO + PDF generator. Verify: build lib + unit test mapper.
2. **Endpoint `/customers/search`** — controller + repo. Verify: Swagger với 4-5 keyword.
3. **Frontend autocomplete component** — `CustomerAutocomplete` + `useCustomersSearch`. Verify: đi qua AC §16 BD.
4. **Tích hợp vào quotation-form-page** — thay Select + thêm field `customerName` + auto-fill logic. Verify: dev server, golden path + edit báo giá cũ.
5. **Quick-add Dialog** — refactor `customer-form-page` tách `CustomerFormFields`, wire vào icon "+". Verify: e2e "thêm nhanh → auto-chọn → focus".
6. **PDF & polish** — verify PDF in đúng `customerName` snapshot; a11y (aria-combobox/listbox/option).

---

## 9. Risks & mitigations

| Rủi ro | Mitigation |
|---|---|
| Search không dấu chậm trên KH lớn | Index trên cột normalize; limit=20; debounce 250ms |
| Quick-add Dialog nested forms | Dialog portal + form độc lập, không nest |
| KH gốc đã inactive khi edit báo giá cũ | Backend allow GET KH inactive theo ID; FE badge "Ngừng sử dụng" |
| Field `customerName` rỗng | FE blur revert về master name; backend fallback khi render PDF |

---

## 10. Open questions

1. Backend `/customers` hiện đang search trên những cột nào? Có cột `NameNoDiacritic` chưa? → Cần xác minh trong phase planning trước khi viết endpoint mới.
2. PDF generator hiện đang dùng tên KH lấy từ đâu (entity navigation hay snapshot field)? → Cần verify để áp dúng đúng fallback logic.
3. Có cần index thêm cột `CustomerNameSnapshot` cho search trong tương lai không? → Hiện trả lời "không" theo decision §4, nhưng nên ghi nhận để revisit.

---

## 11. Next steps

- (Khuyến nghị) Chuyển sang skill `write-plan` để chia bước thực thi theo 6 phase ở §8, mỗi phase có verification checkpoint.
- Trước khi viết plan: trả lời 3 câu ở §10 bằng cách đọc backend `Customers` controller/repository và PDF generator.
