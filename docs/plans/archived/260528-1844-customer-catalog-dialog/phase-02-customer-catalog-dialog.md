# Phase 02 — CustomerCatalogDialog wrapper

**Status:** [ ] pending
**Complexity:** S

## Objective

Tạo component `CustomerCatalogDialog` — dialog wrapper mount `CustomerCatalogList`. Khi user click chọn một khách hàng, dialog gọi `customersApi.get(id)` để lấy đầy đủ thông tin (bao gồm `companyAddress`, `defaultShippingAddress`), map sang `CustomerSearchItem`, gọi `onSelect` callback rồi đóng dialog.

## Files

- `frontend/src/features/customers/components/customer-catalog-dialog.tsx` ← tạo mới
- `frontend/src/features/customers/components/customer-catalog-dialog.test.tsx` ← tạo mới

## Dependency

Phase 01 phải complete (cần `CustomerCatalogList`).

## Tasks

### Task 1 — Viết failing test cho CustomerCatalogDialog

1. Tạo file `frontend/src/features/customers/components/customer-catalog-dialog.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CustomerCatalogDialog } from './customer-catalog-dialog';
import type { Customer, PagedResult, CustomerListItem } from '@/features/customers/types';

const getMock = vi.fn();
const listMock = vi.fn();
vi.mock('@/features/customers/api', () => ({
  customersApi: {
    get: (...args: unknown[]) => getMock(...args),
    list: (...args: unknown[]) => listMock(...args),
    search: vi.fn().mockResolvedValue([]),
  },
}));

const fullCustomer: Customer = {
  id: 'id-1',
  code: 'KH-001',
  name: 'Công ty ABC',
  taxCode: '0100000001',
  companyAddress: '12 Đường Số 1, Q.1, TP.HCM',
  defaultShippingAddress: '34 Nguyễn Huệ',
  contactPerson: 'Anh A',
  phoneNumber: '0901111111',
  email: 'abc@example.com',
  group: 'Company',
  status: 'Active',
  createdAt: '2024-01-01T00:00:00Z',
};

const pagedResult: PagedResult<CustomerListItem> = {
  items: [{ id: 'id-1', code: 'KH-001', name: 'Công ty ABC', taxCode: '0100000001', phoneNumber: '0901111111', contactPerson: 'Anh A', group: 'Company', status: 'Active' }],
  page: 1, pageSize: 20, totalItems: 1, totalPages: 1, hasNextPage: false, hasPreviousPage: false,
};

function renderWithClient(ui: React.ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('CustomerCatalogDialog', () => {
  beforeEach(() => {
    getMock.mockReset();
    listMock.mockReset();
    getMock.mockResolvedValue(fullCustomer);
    listMock.mockResolvedValue(pagedResult);
  });

  it('does not render dialog content when open=false', () => {
    renderWithClient(
      <CustomerCatalogDialog open={false} onOpenChange={vi.fn()} initialQuery="" onSelect={vi.fn()} />,
    );
    expect(screen.queryByRole('dialog')).toBeNull();
  });

  it('renders dialog title when open=true', () => {
    renderWithClient(
      <CustomerCatalogDialog open={true} onOpenChange={vi.fn()} initialQuery="" onSelect={vi.fn()} />,
    );
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('calls customersApi.get and onSelect with full CustomerSearchItem when row is clicked', async () => {
    const onSelect = vi.fn();
    const onOpenChange = vi.fn();
    renderWithClient(
      <CustomerCatalogDialog open={true} onOpenChange={onOpenChange} initialQuery="" onSelect={onSelect} />,
    );
    await waitFor(() => screen.getByText('KH-001'));
    fireEvent.click(screen.getByText('KH-001').closest('tr')!);
    await waitFor(() => expect(getMock).toHaveBeenCalledWith('id-1'));
    await waitFor(() =>
      expect(onSelect).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 'id-1',
          code: 'KH-001',
          name: 'Công ty ABC',
          companyAddress: '12 Đường Số 1, Q.1, TP.HCM',
          defaultShippingAddress: '34 Nguyễn Huệ',
        }),
      ),
    );
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('closes cleanly without error when open changes to false', () => {
    const { rerender } = renderWithClient(
      <CustomerCatalogDialog open={true} onOpenChange={vi.fn()} initialQuery="ABC" onSelect={vi.fn()} />,
    );
    const input = screen.getByPlaceholderText(/tìm mã, tên khách hàng/i) as HTMLInputElement;
    expect(input.value).toBe('ABC');
    rerender(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <CustomerCatalogDialog open={false} onOpenChange={vi.fn()} initialQuery="ABC" onSelect={vi.fn()} />
      </QueryClientProvider>,
    );
    // Dialog closes cleanly without error
    expect(screen.queryByRole('dialog')).toBeNull();
  });
});
```

2. Chạy test để xác nhận FAIL (component chưa tồn tại):
```bash
cd frontend && npx vitest run src/features/customers/components/customer-catalog-dialog.test.tsx 2>&1 | tail -20
```
Expected: FAIL với `Cannot find module './customer-catalog-dialog'`

### Task 2 — Implement CustomerCatalogDialog

3. Tạo `frontend/src/features/customers/components/customer-catalog-dialog.tsx`:

```tsx
import { Dialog, DialogContent, DialogTitle } from '@/components/ui/dialog';
import { customersApi } from '@/features/customers/api';
import { toast } from '@/lib/use-toast';
import type { CustomerSearchItem } from '@/features/customers/types';
import { CustomerCatalogList } from './customer-catalog-list';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialQuery: string;
  onSelect: (c: CustomerSearchItem) => void;
}

export function CustomerCatalogDialog({ open, onOpenChange, initialQuery, onSelect }: Props) {
  async function handleSelectId(id: string) {
    try {
      const customer = await customersApi.get(id);
      onSelect({
        id: customer.id,
        code: customer.code,
        name: customer.name,
        taxCode: customer.taxCode,
        companyAddress: customer.companyAddress,
        defaultShippingAddress: customer.defaultShippingAddress,
        contactPerson: customer.contactPerson,
        phoneNumber: customer.phoneNumber,
        status: customer.status,
      });
      onOpenChange(false);
    } catch {
      toast({ variant: 'destructive', title: 'Không thể tải thông tin khách hàng. Vui lòng thử lại.' });
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-w-4xl overflow-hidden p-0 h-[80vh]" showClose={true}>
        <DialogTitle className="sr-only">Danh mục khách hàng</DialogTitle>
        <div className="flex w-full flex-col min-h-0">
          <div className="border-b px-4 pr-10 py-3 flex-shrink-0">
            <h2 className="text-base font-semibold">Danh mục khách hàng</h2>
          </div>
          <div className="flex-1 overflow-hidden min-h-0">
            <CustomerCatalogList
              initialQuery={initialQuery}
              onSelect={handleSelectId}
            />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

4. Chạy test để xác nhận PASS:
```bash
cd frontend && npx vitest run src/features/customers/components/customer-catalog-dialog.test.tsx 2>&1 | tail -20
```
Expected: PASS — 4 tests passed

5. Commit:
```bash
git add frontend/src/features/customers/components/customer-catalog-dialog.tsx frontend/src/features/customers/components/customer-catalog-dialog.test.tsx
git commit -m "feat: add CustomerCatalogDialog wrapper with full-customer fetch on select"
```

## Verification

```bash
cd frontend && npx vitest run src/features/customers/components/customer-catalog-dialog.test.tsx
```
Expected: 4 tests passed

## Exit Criteria

- Tất cả 4 tests trong `customer-catalog-dialog.test.tsx` PASS
- `npm run typecheck` không có lỗi mới
