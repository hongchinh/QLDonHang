# Phase 08 — Frontend list + form pages + routing

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** L

## Objective
Deliver the user-facing surface: a quotation list with status/customer/date filters, paging, and per-row status actions; a quotation form with the line-items grid (product typeahead suggestion table, per-row computed values), sticky totals panel, header sections, state-action buttons, and PDF download. Wire the routes under `/quotations` with permission guards.

## Files
- `frontend/src/pages/quotations/quotation-list-page.tsx` (new)
- `frontend/src/pages/quotations/quotation-form-page.tsx` (new)
- `frontend/src/pages/quotations/components/line-items-grid.tsx` (new)
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx` (new)
- `frontend/src/pages/quotations/components/totals-panel.tsx` (new)
- `frontend/src/pages/quotations/components/status-pill.tsx` (new)
- `frontend/src/pages/quotations/utils/compute-line.ts` (new — pure FE recompute mirroring backend math, for live preview)
- `frontend/src/pages/quotations/utils/compute-line.test.ts` (new — unit tests for FE recompute)
- `frontend/src/App.tsx` (replace the `<Route path="quotations" ...>` placeholder with real routes)

## Tasks

1. **`status-pill.tsx`** — small wrapper around `Badge` mapping `QuotationStatus → { label, variant }`:
   - `Draft` → secondary `Nháp`
   - `Sent` → outline `Đã gửi`
   - `Confirmed` → success `Đã xác nhận`
   - `ConvertedToOrder` → default `Đã chuyển đơn`
   - `Cancelled` → destructive `Đã hủy`

2. **`compute-line.ts`** — pure functions:
   ```ts
   export function deriveQuantityFromDimensions(line) {
     // PerSquareMeter: L*W*Sheet/1e6 ; PerLinearMeter: L*Sheet/1000 ; PerCubicMeter: L*W*T*Sheet/1e9
     // Return undefined if required dimensions absent.
   }
   export function computeLineTotal(line) { return round2(line.quantity * line.unitPrice); }
   export function computeLineCost(line)  { return line.unitCost != null ? round2(line.quantity * line.unitCost) : undefined; }
   export function computeTotals(lines, header) {
     const subtotal = sum(lines.map(computeLineTotal));
     const totalCost = sum(lines.map(l => computeLineCost(l) ?? 0));
     const taxAmount = round0(subtotal * header.taxRate / 100);
     const total = subtotal - header.discount + header.freight + taxAmount;
     const grossProfit = subtotal - totalCost - header.discount;
     return { subtotal, taxAmount, total, totalCost, grossProfit };
   }
   ```
   Match backend `Recompute` exactly. Includes `round2` (banker's-away-from-zero) and `round0` for VND tax.

3. **`compute-line.test.ts`** — vitest cases:
   - PerUnit: 5 × 12000 = 60000 lineTotal.
   - PerSquareMeter with L=2000 W=1000 Sheet=3 → area 6 m² → lineTotal = 6 × unitPrice.
   - Totals: subtotal 100k, discount 10k, freight 5k, taxRate 10 → taxAmount 10000, total 105000.
   - Total profit: subtotal 100k, totalCost 60k, discount 10k → grossProfit 30000.

4. **`product-typeahead-cell.tsx`** — input + dropdown table inside a `Popover` (use Radix Popover via the existing shadcn primitives; if none present, fall back to absolute-positioned div). Behavior:
   - As user types, debounce 200ms; call `useProductSearch(q)`.
   - Render results as a small table with columns `Mã | Tên | Loại giá | Quy cách | Giá bán`.
   - Pressing Enter / clicking a row commits the selection: emit `onSelect(productSuggestion)` so the parent can hydrate the line's `productId`, `productCode`, `productName`, `specification`, `unitName`, `pricingMode`, `unitPrice` (← `defaultPrice`), `unitCost` (← `costPrice`).
   - Keyboard: ↓/↑ navigates rows; Escape closes the popover.
   - When parent unhydrates (empty), the cell shows the raw text the user typed — supports "freeform" line entries (no productId, user types `productName` manually elsewhere).

5. **`line-items-grid.tsx`** — controlled list. Props: `lines`, `onChange(lines)`. Uses `useFieldArray` from react-hook-form for stable identity. Columns:
   - STT (read-only)
   - Mã hàng (typeahead cell)
   - Tên hàng (text)
   - ĐVT (text)
   - Loại giá (read-only badge derived from line.pricingMode)
   - Kích thước (D × R × C × Sheet, 4 compact inputs)
   - Số lượng (number; auto-fills from `deriveQuantityFromDimensions` when dimensions change, but user can override)
   - Đơn giá (number)
   - Giá vốn (number, optional)
   - Thành tiền (read-only, `computeLineTotal`)
   - Lợi nhuận (read-only, hidden if no cost set)
   - Xóa dòng
   - Bottom "Thêm dòng" button which appends `{ sortOrder: lines.length, pricingMode: 'PerUnit', quantity: 1, unitPrice: 0 }`.

6. **`totals-panel.tsx`** — sticky panel inside a `Card` on the right side of the form (or below on mobile). Shows live totals from `computeTotals(lines, header)`. Fields:
   - Cộng tiền hàng (subtotal)
   - Chiết khấu (input bound to `header.discount`)
   - Cước vận chuyển (input bound to `header.freight`)
   - Thuế suất % (input bound to `header.taxRate`)
   - Tiền thuế (computed)
   - **Tổng cộng** (bold, larger)
   - Tổng giá vốn (internal, always shown per user's earlier decision)
   - Lợi nhuận gộp

7. **`quotation-list-page.tsx`** — patterned after `customer-list-page.tsx`:
   - Filters: `q` search (placeholder "Tìm theo mã / tên khách"), status `Select`, date range (`from`/`to`).
   - Columns: `Số báo giá`, `Ngày`, `Khách hàng`, `SĐT`, `Tổng tiền` (right-aligned `#,##0`), `Trạng thái` (`StatusPill`), `Người lập`, actions (Sửa, In, Hủy).
   - Action menu per row:
     - "In" → triggers `quotationsApi.downloadPdf(id)` + `saveAs(blob, ...)` via a tiny helper (browser anchor click).
     - "Hủy" → confirm dialog → `useTransitionQuotation({ action: 'Cancel' })`.
   - Paging same as customer list.

8. **`quotation-form-page.tsx`** — full form:
   - Top bar: back arrow, title "Thêm báo giá" / "Chỉnh sửa báo giá", and state-action buttons on the right (Send / Confirm / Cancel — visibility derived from current status).
   - Section 1 "Thông tin khách hàng": customer `Select` (data from `useCustomers` paginated; for MVP just call `useCustomers({ page: 1, pageSize: 100 })` and render a flat select — switch to async search later). On change, snapshot fields autofill from the customer's address/contact.
   - Section 2 "Thông tin giao hàng": delivery address + recipient + phone + date + note (free text).
   - Section 3 "Chi tiết hàng hóa": `<LineItemsGrid />`.
   - Section 4 (sidebar / sticky): `<TotalsPanel />`.
   - Submit: `useCreateQuotation` / `useUpdateQuotation`; on success, navigate to detail (`/quotations/:id`).
   - Resolver cast pattern same as the Products form (`as unknown as Resolver<...>`) — `optionalNumber`/`z.coerce.number` produce asymmetric input/output.

9. **App.tsx** — replace the placeholder:
   ```tsx
   <Route path="quotations">
     <Route index element={
       <ProtectedRoute permission="quotations.view"><QuotationListPage /></ProtectedRoute>
     } />
     <Route path="new" element={
       <ProtectedRoute permission="quotations.create"><QuotationFormPage /></ProtectedRoute>
     } />
     <Route path=":id" element={
       <ProtectedRoute permission="quotations.update"><QuotationFormPage /></ProtectedRoute>
     } />
   </Route>
   ```
   Import `QuotationListPage` and `QuotationFormPage`.

## Verification
```
cd frontend && npm run build && npm test -- --run
```

Manual smoke (with backend running):
1. Log in as ADMIN. Navigate `/quotations` → "Thêm báo giá".
2. Pick a customer; observe autofilled snapshot fields.
3. Add 2 lines: one PerUnit (e.g. unit "Cái", qty 3, price 10000 → total 30000), one PerSquareMeter (length 2000, width 1000, sheets 4 → qty 8 m², price 50000 → total 400000).
4. Discount 10000, freight 20000, taxRate 8 → confirm preview matches the formula.
5. Save → status Draft. "Gửi" → Sent. "Xác nhận" → Confirmed.
6. "In" → PDF downloads, opens, layout looks right, currency formatted `#,##0`.
7. Without `quotations.create` permission (login as ACCOUNTANT) the Add button is hidden; navigation to `/quotations/new` is blocked.

## Exit Criteria
- `npm run build` and `npm test -- --run` pass.
- `compute-line.test.ts` covers all four pricing modes + totals math.
- Manual smoke succeeds end-to-end through Send / Confirm / Print.
- No console warnings about controlled/uncontrolled inputs or React keys.
