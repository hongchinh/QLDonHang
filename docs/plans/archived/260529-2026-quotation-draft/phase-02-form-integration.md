# Phase 02 — Form integration

**Status:** [ ] pending
**Complexity:** M

## Objective

Tích hợp `useQuotationDraft` vào `QuotationFormInner` trong `quotation-form-page.tsx`:
1. Đọc draft synchronously khi component mount, dùng làm `defaultValues` cho `useForm`
2. Phục hồi `selectedCustomerView` từ draft nếu có
3. Gọi `useQuotationDraft` để set up debounced watcher
4. Gọi `clearDraft()` sau khi create thành công
5. Hiển thị badge "Nháp chưa lưu từ HH:mm" kèm nút "Xóa nháp"

## Files

- `frontend/src/pages/quotations/quotation-form-page.tsx` (sửa)
- `frontend/src/pages/quotations/quotation-form-page.draft.test.tsx` (tạo mới)

## Tasks

### Task 2.1 — Viết integration test kiểm tra draft badge và giá trị form

1. Tạo file `frontend/src/pages/quotations/quotation-form-page.draft.test.tsx`:

```tsx
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QuotationFormPage } from './quotation-form-page';
import { writeQuotationDraft } from '@/features/quotations/use-quotation-draft';
import type { QuotationFormValues } from '@/features/quotations/schema';
import React from 'react';

// useAuthStore.getState() is called in component body (not as hook).
// Mock the module-level export so getState() returns a stable user.
vi.mock('@/stores/auth-store', () => ({
  useAuthStore: Object.assign(
    (selector: (s: { user: { id: string } | null; permissions: string[] }) => unknown) =>
      selector({ user: { id: 'user-test' }, permissions: [] }),
    {
      getState: () => ({ user: { id: 'user-test' } }),
    },
  ),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    // Route for new-quotation form — id='new' means isEdit=false
    useParams: () => ({ id: 'new' }),
    useNavigate: () => vi.fn(),
    Link: ({ children, to, ...rest }: { children: React.ReactNode; to: string }) =>
      React.createElement('a', { href: String(to), ...rest }, children),
  };
});

vi.mock('@/features/quotations/hooks', () => ({
  useQuotation: () => ({ data: undefined, isLoading: false }),
  useCreateQuotation: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
    isError: false,
    error: null,
  }),
  useUpdateQuotation: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
    isError: false,
    error: null,
  }),
  useTransitionQuotation: () => ({
    mutateAsync: vi.fn(),
    isPending: false,
    isError: false,
    error: null,
  }),
  useCloneQuotation: () => ({ mutateAsync: vi.fn(), isPending: false }),
  useQuotationActivities: () => ({
    data: [],
    isLoading: false,
    isError: false,
    error: null,
    refetch: vi.fn(),
  }),
}));

vi.mock('@/features/customers/hooks', () => ({
  useCustomer: () => ({ data: undefined }),
}));

// CustomerAutocomplete makes network calls — stub it to avoid test complexity
vi.mock('@/components/customer-autocomplete/customer-autocomplete', () => ({
  CustomerAutocomplete: () => React.createElement('div', { 'data-testid': 'customer-autocomplete' }),
}));

vi.mock('@/components/customer-autocomplete/customer-quick-add-dialog', () => ({
  CustomerQuickAddDialog: () => null,
}));

vi.mock('@/components/auth/can', () => ({
  Can: ({ children }: { children: React.ReactNode }) => React.createElement(React.Fragment, null, children),
}));

// LineItemsGrid uses getBoundingClientRect and complex grid navigation — jsdom has no layout.
// TotalsPanel depends on computed line totals — stub both to isolate draft behavior.
vi.mock('./components/line-items-grid', () => ({
  LineItemsGrid: React.forwardRef((_props: unknown, _ref: unknown) =>
    React.createElement('div', { 'data-testid': 'line-items-grid' }),
  ),
}));

vi.mock('./components/totals-panel', () => ({
  TotalsPanel: () => React.createElement('div', { 'data-testid': 'totals-panel' }),
}));

const DRAFT_VALUES: QuotationFormValues = {
  customerId: 'cust-001',
  customerName: 'Khách hàng nháp',
  quotationDate: '2026-05-29',
  deliveryAddress: 'Địa chỉ nháp',
  deliveryRecipient: '',
  deliveryPhone: '',
  transportVehicleNumber: '',
  deliveryDate: '2026-05-30',
  deliveryNote: '',
  taxRate: 10,
  discount: 0,
  freight: 0,
  advancePayment: 0,
  internalNote: '',
  lines: [
    {
      _uiKey: 'key1',
      sortOrder: 0,
      productName: 'Hàng nháp A',
      unitName: 'Cái',
      pricingMode: 'PerUnit',
      quantity: 2,
      unitPrice: 50000,
    },
  ],
};

describe('QuotationFormPage — draft restore', () => {
  beforeEach(() => localStorage.clear());

  it('does not show draft badge when no draft in localStorage', () => {
    render(<QuotationFormPage />);
    expect(screen.queryByText(/nháp chưa lưu/i)).toBeNull();
  });

  it('shows draft badge when draft exists in localStorage', async () => {
    writeQuotationDraft('user-test', DRAFT_VALUES, null);
    render(<QuotationFormPage />);
    await waitFor(() => {
      expect(screen.getByText(/nháp chưa lưu/i)).toBeInTheDocument();
    });
  });

  it('pre-fills customerName from draft', async () => {
    writeQuotationDraft('user-test', DRAFT_VALUES, null);
    render(<QuotationFormPage />);
    await waitFor(() => {
      const input = screen.getByRole<HTMLInputElement>('textbox', { name: /tên kh/i });
      expect(input.value).toBe('Khách hàng nháp');
    });
  });
});
```

2. Chạy test để xác nhận FAIL:
   ```bash
   cd frontend && npx vitest run src/pages/quotations/quotation-form-page.draft.test.tsx
   ```
   Expected: FAIL — component chưa đọc draft, không có badge

### Task 2.2 — Tích hợp draft vào `quotation-form-page.tsx`

Sửa `frontend/src/pages/quotations/quotation-form-page.tsx` theo thứ tự sau:

**A. Thêm imports** — sau dòng `import { toast } from '@/lib/use-toast';`:
```typescript
import { useAuthStore } from '@/stores/auth-store';
import {
  readQuotationDraft,
  useQuotationDraft,
  type QuotationDraftCustomer,
  type QuotationDraftStorage,
} from '@/features/quotations/use-quotation-draft';
```

**B. Trong `QuotationFormInner`, TRƯỚC `useForm`** — thêm ngay sau dòng `const navigateInner = useNavigate();` và `const clone = useCloneQuotation();`:

```typescript
// Read draft and userId exactly once at mount via useState initializer.
// useAuthStore.getState() is Zustand's static getter — NOT a React hook,
// so calling it inside a useState initializer is valid and won't trip rules-of-hooks.
const [mountData] = useState<{ draft: QuotationDraftStorage | null; userId: string }>(() => {
  const uid = useAuthStore.getState().user?.id ?? '';
  return { draft: !isEdit ? readQuotationDraft(uid) : null, userId: uid };
});
```

**C. Sửa `useForm` `defaultValues`** — tìm:
```typescript
    defaultValues: toFormDefaults(initial),
```
Thay bằng:
```typescript
    defaultValues: (!isEdit && mountData.draft?.values) ? mountData.draft.values : toFormDefaults(initial),
```

**D. Sửa `selectedCustomerView` useState** — tìm đoạn khởi tạo `useState` cho `selectedCustomerView` (khoảng dòng 281-290), thay callback initializer thành:
```typescript
  const [selectedCustomerView, setSelectedCustomerView] = useState<{ id: string; code: string; name: string } | null>(
    () => {
      if (!isEdit && mountData.draft?.selectedCustomer) return mountData.draft.selectedCustomer;
      return initialSelectedCustomer
        ? { id: initialSelectedCustomer.id, code: initialSelectedCustomer.code, name: initialSelectedCustomer.name }
        : null;
    },
  );
```

**E. Thêm `useQuotationDraft` hook** — sau dòng `const [pendingButtonAction, setPendingButtonAction] = ...`:
```typescript
  const { hasDraft, draftSavedAt, clearDraft } = useQuotationDraft({
    form,
    userId: mountData.userId,
    isEdit,
    getSelectedCustomer: () => selectedCustomerView as QuotationDraftCustomer | null,
    initialHasDraft: !!mountData.draft,
    initialSavedAt: mountData.draft ? new Date(mountData.draft.savedAt) : null,
  });
```

**F. Sửa `submitWithIntent`** — trong async callback của `form.handleSubmit`, tìm:
```typescript
        try {
          await onSubmit(parsed, intent);
        } finally {
```
Thay bằng:
```typescript
        try {
          await onSubmit(parsed, intent);
          if (!isEdit) clearDraft();
        } finally {
```

**G. Thêm helper `formatDraftTime`** — ở cuối file, cùng khu vực với `actionLabel`:
```typescript
function formatDraftTime(date: Date): string {
  return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}
```

**H. Thêm `X` vào import lucide-react** — tìm dòng import lucide ở đầu file và thêm `X` vào destructure:
```typescript
import {
  ArrowLeft,
  // ... các icon hiện có ...
  X,
} from 'lucide-react';
```

**I. Thêm badge JSX** — trong phần JSX của `QuotationFormInner`, tìm đoạn kết thúc block `isEdit && initial && !initial.canEdit` (khoảng dòng 667), ngay TRƯỚC `{/* eslint-disable-next-line ... */}` trước `<form`, thêm:
```tsx
      {!isEdit && hasDraft && (
        <div className="flex items-center gap-3 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-900">
          <span>
            Nháp chưa lưu{draftSavedAt ? ` từ ${formatDraftTime(draftSavedAt)}` : ''}
          </span>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-auto px-2 py-0.5 text-xs text-amber-700 hover:text-amber-900"
            onClick={() => {
              clearDraft();
              form.reset(toFormDefaults(undefined));
              setSelectedCustomerView(null);
            }}
          >
            <X className="mr-1 h-3 w-3" />
            Xóa nháp
          </Button>
        </div>
      )}
```

3. Chạy test để xác nhận PASS:
   ```bash
   cd frontend && npx vitest run src/pages/quotations/quotation-form-page.draft.test.tsx
   ```
   Expected: tất cả 3 tests PASS

4. Kiểm tra TypeScript và toàn bộ test suite:
   ```bash
   cd frontend && npx tsc --noEmit
   cd frontend && npx vitest run
   ```
   Expected: không có lỗi type, tất cả tests PASS

5. Commit:
   ```bash
   git add frontend/src/pages/quotations/quotation-form-page.tsx frontend/src/pages/quotations/quotation-form-page.draft.test.tsx
   git commit -m "feat: restore quotation draft on new-form mount with amber badge"
   ```

## Verification

```bash
cd frontend && npx vitest run
```

Kiểm tra thủ công (chạy dev server):
1. Mở `/quotations/new`, nhập "Tên KH" = "Khách hàng test", thêm 1 dòng hàng
2. Chờ ~2 giây (debounce), rồi điều hướng sang `/quotations`
3. Click "Thêm báo giá" → form phải hiện dữ liệu cũ + badge vàng "Nháp chưa lưu từ HH:mm"
4. Kiểm tra `localStorage` trong DevTools → key `quotation_draft_{userId}` tồn tại
5. Click "Xóa nháp" → form reset trống, badge biến mất, localStorage key xóa
6. Nhập lại, nhấn "Lưu và thoát" → success → mở lại "Thêm báo giá" → form trống
7. Mở `/quotations/:someId` (edit mode) → badge không hiển thị dù localStorage có draft

## Exit Criteria

- [ ] 3 integration tests trong `quotation-form-page.draft.test.tsx` PASS
- [ ] Toàn bộ test suite PASS
- [ ] TypeScript không báo lỗi
- [ ] Badge "Nháp chưa lưu từ HH:mm" hiển thị khi có draft (verified thủ công)
- [ ] Form được điền lại với draft values khi mở `/quotations/new` (verified thủ công)
- [ ] Click "Xóa nháp" → form trống, badge ẩn, localStorage key xóa (verified thủ công)
- [ ] Save thành công → localStorage key bị xóa (verified thủ công)
- [ ] Edit mode không có badge (verified thủ công)
