import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 }
    }
  });
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
    const user = userEvent.setup();
    const onSelect = vi.fn();
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={onSelect} />);
    await waitFor(() => screen.getByText('KH-001'));
    await user.click(screen.getByText('KH-001').closest('tr')!);
    expect(onSelect).toHaveBeenCalledWith('id-1');
  });

  it('clicking Đại lý tab calls list with group=Agent', async () => {
    const user = userEvent.setup();
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={vi.fn()} />);
    await waitFor(() => screen.getByText('KH-001'));
    const callsBeforeClick = listMock.mock.calls.length;
    await user.click(screen.getByRole('tab', { name: /đại lý/i }));
    await waitFor(() => {
      expect(listMock.mock.calls.length).toBeGreaterThan(callsBeforeClick);
    });
    const newCall = listMock.mock.calls[callsBeforeClick][0];
    expect(newCall.group).toBe('Agent');
  });

  it('shows pagination info', async () => {
    renderWithClient(<CustomerCatalogList initialQuery="" onSelect={vi.fn()} />);
    await waitFor(() => {
      expect(screen.getByText(/trang 1 \/ 1/i)).toBeInTheDocument();
    });
  });
});
