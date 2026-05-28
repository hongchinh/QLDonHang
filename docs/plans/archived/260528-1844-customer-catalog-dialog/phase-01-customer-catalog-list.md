# Phase 01 — CustomerCatalogList component

**Status:** [ ] pending
**Complexity:** M

## Objective

Tạo component `CustomerCatalogList` — list phân trang có search input và group tabs, hiển thị danh sách khách hàng đang hoạt động. Click dòng gọi `onSelect(id)`.

## Files

- `frontend/src/features/customers/components/customer-catalog-list.tsx` ← tạo mới
- `frontend/src/features/customers/components/customer-catalog-list.test.tsx` ← tạo mới

## Tasks

### Task 1 — Viết failing test cho CustomerCatalogList

1. Tạo file `frontend/src/features/customers/components/customer-catalog-list.test.tsx` với nội dung:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CustomerCatalogList } from './customer-catalog-list';
import type { PagedResult, CustomerListItem } from '@/features/customers/types';

const listMock = vi.fn();
vi.mock('@/features/customers/api', () => ({
  customersApi: {
    list: (...args: unknown[]) => listMock(...args),
  },
}));

const sampleItems: CustomerListItem[] = [
  { id: 'id-1', code: 'KH-001', name: 'Công ty ABC', taxCode: '0100000001', phoneNumber: '0901', contactPerson: 'Anh A', group: 'Company', status: 'Active' },
  { id: 'id-2', code: 'KH-002', name: 'Đại lý XYZ', taxCode: '0100000002', phoneNumber: '0902', contactPerson: 'Chị B', group: 'Agent', status: 'Active' },
];

const pagedResult: PagedResult<CustomerListItem> = {
  items: sampleItems,
  page: 1,
  pageSize: 20,
  totalItems: 2,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
};

function renderWithClient(ui: React.ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe('CustomerCatalogList', () => {
  beforeEach(() => {
    listMock.mockReset();
    listMock.mockResolvedValue(pagedResult);
  });

  it('renders search input pre-filled with initialQuery', () => {
    renderWithClient(<CustomerCatalogList initialQuery="ABC" onSelect={vi.fn()} />);
    const input = screen.getByPlaceholderText(/tìm mã, tên khách hàng/i) as HTMLInputElement;
    expect(input.value).toBe('ABC');
  });

  it('renders all 5 group tabs', () => {
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={vi.fn()} />);
    expect(screen.getByRole('tab', { name: /tất cả/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /công ty/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /đại lý/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /khách lẻ/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /công trình/i })).toBeInTheDocument();
  });

  it('renders rows from API result', async () => {
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={vi.fn()} />);
    await waitFor(() => {
      expect(screen.getByText('KH-001')).toBeInTheDocument();
      expect(screen.getByText('KH-002')).toBeInTheDocument();
    });
  });

  it('calls onSelect with customer id when row is clicked', async () => {
    const onSelect = vi.fn();
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={onSelect} />);
    await waitFor(() => screen.getByText('KH-001'));
    fireEvent.click(screen.getByText('KH-001').closest('tr')!);
    expect(onSelect).toHaveBeenCalledWith('id-1');
  });

  it('clicking Đại lý tab calls list with group=Agent', async () => {
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={vi.fn()} />);
    await waitFor(() => screen.getByText('KH-001'));
    fireEvent.click(screen.getByRole('tab', { name: /đại lý/i }));
    await waitFor(() => {
      const lastCall = listMock.mock.calls[listMock.mock.calls.length - 1][0];
      expect(lastCall.group).toBe('Agent');
    });
  });

  it('shows pagination info', async () => {
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={vi.fn()} />);
    await waitFor(() => {
      expect(screen.getByText(/trang 1 \/ 1/i)).toBeInTheDocument();
    });
  });
});
```

2. Chạy test để xác nhận FAIL (component chưa tồn tại):
```bash
cd frontend && npx vitest run src/features/customers/components/customer-catalog-list.test.tsx 2>&1 | tail -20
```
Expected: FAIL với `Cannot find module './customer-catalog-list'`

### Task 2 — Implement CustomerCatalogList

3. Tạo `frontend/src/features/customers/components/customer-catalog-list.tsx`:

```tsx
import { useState } from 'react';
import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useCustomers } from '@/features/customers/hooks';
import { useDebouncedValue } from '@/lib/use-debounced-value';
import { cn } from '@/lib/utils';
import type { CustomerGroup } from '@/features/customers/types';

const GROUP_LABELS: Record<CustomerGroup, string> = {
  Company: 'Công ty',
  Agent: 'Đại lý',
  Retail: 'Khách lẻ',
  Project: 'Công trình',
};

const ALL_GROUPS: CustomerGroup[] = ['Company', 'Agent', 'Retail', 'Project'];

interface Props {
  initialQuery: string;
  onSelect: (id: string) => void;
}

export function CustomerCatalogList({ initialQuery, onSelect }: Props) {
  const [query, setQuery] = useState(initialQuery);
  const [group, setGroup] = useState<CustomerGroup | undefined>(undefined);
  const [page, setPage] = useState(1);

  const debouncedQuery = useDebouncedValue(query, 250);

  const { data, isLoading } = useCustomers({
    page,
    pageSize: 20,
    search: debouncedQuery || undefined,
    group: group,
    status: 'Active',
  });

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  function handleGroupChange(value: string) {
    setGroup(value === 'all' ? undefined : (value as CustomerGroup));
    setPage(1);
  }

  return (
    <div className="flex h-full flex-col">
      {/* Search */}
      <div className="border-b p-3">
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            className="pl-8"
            placeholder="Tìm mã, tên khách hàng..."
            value={query}
            onChange={(e) => { setQuery(e.target.value); setPage(1); }}
            autoFocus
          />
        </div>
      </div>

      {/* Group tabs */}
      <div className="border-b px-3 py-2 overflow-x-auto">
        <Tabs value={group ?? 'all'} onValueChange={handleGroupChange}>
          <TabsList className="h-auto flex-wrap gap-1 bg-transparent p-0">
            <TabsTrigger value="all" className="h-7 px-2 text-xs">Tất cả</TabsTrigger>
            {ALL_GROUPS.map((g) => (
              <TabsTrigger key={g} value={g} className="h-7 px-2 text-xs">
                {GROUP_LABELS[g]}
              </TabsTrigger>
            ))}
          </TabsList>
        </Tabs>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-auto">
        <table className="w-full text-sm">
          <thead className="sticky top-0 bg-muted/80 backdrop-blur-sm">
            <tr className="border-b">
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">Mã KH</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">Tên</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">MST</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">Người LH</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">SĐT</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground">Nhóm</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <>
                {Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i} className="border-b animate-pulse">
                    <td colSpan={6} className="px-3 py-2">
                      <div className="h-4 rounded bg-muted" />
                    </td>
                  </tr>
                ))}
              </>
            )}
            {!isLoading && items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-8 text-center text-muted-foreground">
                  Không tìm thấy khách hàng phù hợp
                </td>
              </tr>
            )}
            {!isLoading &&
              items.map((item) => (
                <tr
                  key={item.id}
                  className={cn('cursor-pointer border-b transition-colors hover:bg-muted/50')}
                  onClick={() => onSelect(item.id)}
                >
                  <td className="px-3 py-2 font-mono text-xs">{item.code}</td>
                  <td className="px-3 py-2">{item.name}</td>
                  <td className="px-3 py-2 text-muted-foreground">{item.taxCode ?? '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{item.contactPerson ?? '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{item.phoneNumber ?? '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{GROUP_LABELS[item.group]}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between border-t px-3 py-2 text-sm">
        <span className="text-muted-foreground">
          Trang {page} / {totalPages}
          {data && <span className="ml-2 text-xs">({data.totalItems} khách hàng)</span>}
        </span>
        <div className="flex gap-1">
          <button
            type="button"
            className="rounded border px-2 py-1 text-xs disabled:opacity-40 hover:bg-muted"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            ← Trước
          </button>
          <button
            type="button"
            className="rounded border px-2 py-1 text-xs disabled:opacity-40 hover:bg-muted"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Sau →
          </button>
        </div>
      </div>
    </div>
  );
}
```

4. Chạy test để xác nhận PASS:
```bash
cd frontend && npx vitest run src/features/customers/components/customer-catalog-list.test.tsx 2>&1 | tail -20
```
Expected: PASS — 6 tests passed

5. Commit:
```bash
git add frontend/src/features/customers/components/customer-catalog-list.tsx frontend/src/features/customers/components/customer-catalog-list.test.tsx
git commit -m "feat: add CustomerCatalogList component with search and group filter tabs"
```

## Verification

```bash
cd frontend && npx vitest run src/features/customers/components/customer-catalog-list.test.tsx
```
Expected: 6 tests passed

## Exit Criteria

- Tất cả 6 tests trong `customer-catalog-list.test.tsx` PASS
- `npm run typecheck` không có lỗi mới
