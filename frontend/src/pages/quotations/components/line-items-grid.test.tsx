import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useForm } from 'react-hook-form';
import { LineItemsGrid } from './line-items-grid';
import type {
  QuotationFormParsed,
  QuotationFormValues,
  QuotationLineFormValues,
} from '@/features/quotations/schema';

vi.mock('./product-typeahead-cell', () => ({
  ProductTypeaheadCell: ({ value, onChange }: { value: string; onChange: (value: string) => void }) => (
    <input
      aria-label="product-code"
      value={value}
      onChange={(e) => onChange((e.target as HTMLInputElement).value)}
    />
  ),
}));

const baseLines: QuotationLineFormValues[] = [
  {
    sortOrder: 0,
    productCode: 'A001',
    productName: 'Alpha',
    unitName: 'pcs',
    pricingMode: 'PerUnit',
    quantity: 2,
    unitPrice: 100,
  },
  {
    sortOrder: 1,
    productCode: 'B001',
    productName: 'Beta',
    unitName: 'pcs',
    pricingMode: 'PerUnit',
    quantity: 1,
    unitPrice: 50,
  },
];

function renderGrid(lines: QuotationLineFormValues[] = baseLines) {
  function Harness() {
    const form = useForm<QuotationFormValues, unknown, QuotationFormParsed>({
      defaultValues: {
        customerId: '11111111-1111-1111-1111-111111111111',
        customerName: '',
        quotationDate: '2026-05-13',
        deliveryAddress: '',
        deliveryRecipient: '',
        deliveryPhone: '',
        deliveryDate: '',
        deliveryNote: '',
        taxRate: 0,
        discount: 0,
        freight: 0,
        internalNote: '',
        lines,
      },
    });
    return <LineItemsGrid form={form} />;
  }

  return render(<Harness />);
}

describe('LineItemsGrid', () => {
  it('renders existing rows and subtotal footer', () => {
    renderGrid();

    expect(screen.getByDisplayValue('Alpha')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Beta')).toBeInTheDocument();
    expect(screen.getByText(/Tổng:\s*250\s*₫/)).toBeInTheDocument();
  });

  it('adds a row when clicking add line', async () => {
    renderGrid([baseLines[0]]);

    fireEvent.click(screen.getByRole('button', { name: /thêm dòng/i }));

    await waitFor(() => {
      expect(screen.getAllByLabelText('product-code')).toHaveLength(2);
    });
  });

  it('removes a single row with row trash button', async () => {
    renderGrid();

    fireEvent.click(screen.getAllByRole('button', { name: /xóa dòng/i })[0]);

    await waitFor(() => {
      expect(screen.queryByDisplayValue('Alpha')).not.toBeInTheDocument();
      expect(screen.getByDisplayValue('Beta')).toBeInTheDocument();
    });
  });

  it('clears all rows after confirm dialog confirmation', async () => {
    renderGrid();

    fireEvent.click(screen.getByRole('button', { name: /xóa tất cả dòng/i }));
    expect(screen.getByText('Xóa tất cả dòng?')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Xóa' }));

    await waitFor(() => {
      expect(screen.getByText(/chưa có dòng nào/i)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /xóa tất cả dòng/i })).toBeDisabled();
    });
  });

  it('removes active row with Ctrl+Delete', async () => {
    renderGrid();
    const betaInput = screen.getByDisplayValue('Beta');

    fireEvent.focus(betaInput);
    fireEvent.keyDown(betaInput, { key: 'Delete', ctrlKey: true });

    await waitFor(() => {
      expect(screen.getByDisplayValue('Alpha')).toBeInTheDocument();
      expect(screen.queryByDisplayValue('Beta')).not.toBeInTheDocument();
    });
  });
});
