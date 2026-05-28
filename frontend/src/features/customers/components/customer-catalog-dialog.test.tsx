import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
  const client = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 }
    }
  });
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
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('renders dialog title when open=true', () => {
    renderWithClient(
      <CustomerCatalogDialog open={true} onOpenChange={vi.fn()} initialQuery="" onSelect={vi.fn()} />,
    );
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('calls customersApi.get and onSelect with full CustomerSearchItem when row is clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    const onOpenChange = vi.fn();
    renderWithClient(
      <CustomerCatalogDialog open={true} onOpenChange={onOpenChange} initialQuery="" onSelect={onSelect} />,
    );
    await waitFor(() => screen.getByText('KH-001'));
    await user.click(screen.getByText('KH-001').closest('tr')!);
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
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0, staleTime: 0 } } })}>
        <CustomerCatalogDialog open={false} onOpenChange={vi.fn()} initialQuery="ABC" onSelect={vi.fn()} />
      </QueryClientProvider>,
    );
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
