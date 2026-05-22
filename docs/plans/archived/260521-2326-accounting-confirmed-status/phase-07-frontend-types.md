# Phase 07 — Frontend Types & Status Pill

**Status:** [ ] pending
**Complexity:** S

## Objective

Cập nhật TypeScript types để phản ánh enum mới và cập nhật `StatusPill` component với màu cho `AccountingConfirmed`.

## Files

- `frontend/src/features/quotations/types.ts`
- `frontend/src/lib/permissions.ts`
- `frontend/src/pages/quotations/components/status-pill.tsx`

## Tasks

### types.ts

1. **`QuotationStatus`** — thêm `'AccountingConfirmed'` vào union type:
   ```typescript
   export type QuotationStatus = 'Draft' | 'Sent' | 'Confirmed' | 'AccountingConfirmed' | 'Cancelled';
   ```

2. **`QuotationAction`** — thêm `'AccountingConfirm'`:
   ```typescript
   export type QuotationAction = 'Send' | 'Confirm' | 'AccountingConfirm' | 'Cancel';
   ```

3. **`QuotationActivityAction`** — thêm `'AccountingConfirmed'`:
   ```typescript
   export type QuotationActivityAction =
     | 'Created'
     | 'Updated'
     | 'Sent'
     | 'Confirmed'
     | 'AccountingConfirmed'
     | 'Cancelled'
     | 'OwnerTransferred'
     | 'Cloned';
   ```

4. **`Quotation` interface** — thêm 3 fields sau `cancelledAt?`:
   ```typescript
   accountingConfirmedAt?: string;
   accountingConfirmedByUserId?: string;
   accountingConfirmedByName?: string;
   ```

5. **`QuotationListItem` interface** — thêm 1 field sau `confirmedAt?`:
   ```typescript
   accountingConfirmedAt?: string;
   ```

### permissions.ts

6. **`PERMISSIONS` array** — thêm 2 permission strings:
   ```typescript
   'quotations.accounting_confirm',
   'quotations.cancel_accounting_confirmed',
   ```
   Đặt sau `'quotations.bypass_lock'` để sync thứ tự với `Permissions.cs`.

### status-pill.tsx

7. **`MAP` object** — thêm entry cho `AccountingConfirmed`:
   ```typescript
   AccountingConfirmed: { label: 'KT xác nhận', variant: 'info' },
   ```
   **Lưu ý**: Kiểm tra `BadgeProps['variant']` xem có `'info'` không. Nếu không có, dùng `'default'` hoặc thêm `'info'` vào Badge component. Xem file `frontend/src/components/ui/badge.tsx` để xác nhận variants có sẵn. Nếu thiếu, dùng `'outline'` với style phân biệt, hoặc thêm `info` variant vào `badge.tsx`.

## Verification

```bash
cd frontend && npm run build
```

TypeScript compile không có lỗi liên quan đến `QuotationStatus`, `QuotationAction`, hoặc `Permission` type.

## Exit Criteria

- Frontend build thành công
- `QuotationStatus` type bao gồm `'AccountingConfirmed'`
- `QuotationAction` type bao gồm `'AccountingConfirm'`
- `permissions.ts` có 2 permission strings mới
- `StatusPill` render đúng label "KT xác nhận" cho status `AccountingConfirmed`
