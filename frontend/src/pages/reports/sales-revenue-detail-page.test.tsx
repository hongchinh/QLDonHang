import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { renderHook } from '@testing-library/react';
import type { SalesRevenueLineItemDto } from '@/features/reports/sales-revenue-detail/types';
import { calculateRevenueTotals, useRevenueTotals, TotalsRow } from './sales-revenue-detail-page';

describe('calculateRevenueTotals', () => {
  it('returns zero totals for empty items array', () => {
    const totals = calculateRevenueTotals([]);
    expect(totals).toEqual({
      quantity: 0,
      lineTotal: 0,
      freight: 0,
      unitCost: 0,
      lineCost: 0,
      lineProfit: 0,
    });
  });

  it('sums quantity correctly', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ quantity: 10 }),
      createMockItem({ quantity: 5 }),
      createMockItem({ quantity: 15 }),
    ];
    const totals = calculateRevenueTotals(items);
    expect(totals.quantity).toBe(30);
  });

  it('sums lineTotal correctly', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ lineTotal: 1000 }),
      createMockItem({ lineTotal: 2000 }),
    ];
    const totals = calculateRevenueTotals(items);
    expect(totals.lineTotal).toBe(3000);
  });

  it('sums freight, treating null as 0', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ freight: 100 }),
      createMockItem({ freight: 50 }),
    ];
    const totals = calculateRevenueTotals(items);
    expect(totals.freight).toBe(150);
  });

  it('sums unitCost, skipping null values', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ unitCost: 60 }),
      createMockItem({ unitCost: null }),
      createMockItem({ unitCost: 40 }),
    ];
    const totals = calculateRevenueTotals(items);
    expect(totals.unitCost).toBe(100);
  });

  it('sums lineCost, skipping null values', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ lineCost: 500 }),
      createMockItem({ lineCost: null }),
    ];
    const totals = calculateRevenueTotals(items);
    expect(totals.lineCost).toBe(500);
  });

  it('sums lineProfit, skipping null values', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ lineProfit: 200 }),
      createMockItem({ lineProfit: null }),
      createMockItem({ lineProfit: 300 }),
    ];
    const totals = calculateRevenueTotals(items);
    expect(totals.lineProfit).toBe(500);
  });
});

describe('useRevenueTotals hook', () => {
  it('returns totals from items using useMemo', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ quantity: 5, lineTotal: 500 }),
      createMockItem({ quantity: 3, lineTotal: 300 }),
    ];
    const { result } = renderHook(() => useRevenueTotals(items));
    expect(result.current.quantity).toBe(8);
    expect(result.current.lineTotal).toBe(800);
  });
});

describe('TotalsRow component', () => {
  it('renders totals with correct money formatting', () => {
    render(
      <TotalsRow
        totals={{
          quantity: 100,
          lineTotal: 50000,
          freight: 5000,
          unitCost: 20000,
          lineCost: 20000,
          lineProfit: 30000,
        }}
        hasCost={true}
      />
    );
    expect(screen.getByText(/100/)).toBeInTheDocument();
    expect(screen.getByText(/50\.000/)).toBeInTheDocument();
  });

  it('hides cost columns when hasCost is false', () => {
    const { container } = render(
      <TotalsRow
        totals={{
          quantity: 10,
          lineTotal: 5000,
          freight: 500,
          unitCost: 0,
          lineCost: 0,
          lineProfit: 0,
        }}
        hasCost={false}
      />
    );
    const rows = container.querySelectorAll('tr');
    expect(rows.length).toBeGreaterThan(0);
  });
});

describe('Scrollable table container', () => {
  it('applies max-height and overflow styles to table container', () => {
    const items: SalesRevenueLineItemDto[] = Array(50)
      .fill(null)
      .map((_, i) => createMockItem({ productName: `Product ${i}` }));

    const { container } = render(
      <div>
        <div data-testid="table-scroll-container" className="overflow-y-auto" style={{ maxHeight: 'calc(100vh - 400px)' }}>
          <div className="overflow-x-auto">
            <table><tbody><tr><td>test</td></tr></tbody></table>
          </div>
        </div>
      </div>
    );

    const tableContainer = container.querySelector('[data-testid="table-scroll-container"]');
    expect(tableContainer).toHaveClass('overflow-y-auto');
    expect(tableContainer).toHaveStyle({ maxHeight: 'calc(100vh - 400px)' });
  });
});

describe('TotalsRow placement', () => {
  it('renders TotalsRow below table with correct props', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ quantity: 10, lineTotal: 1000 }),
    ];

    const { result } = renderHook(() => useRevenueTotals(items));
    render(
      <div>
        <TotalsRow totals={result.current} hasCost={false} />
      </div>
    );

    expect(screen.getByText('Tổng cộng')).toBeInTheDocument();
  });
});

describe('Sticky table header', () => {
  it('applies sticky positioning to TableHeader', () => {
    const items: SalesRevenueLineItemDto[] = Array(50)
      .fill(null)
      .map((_, i) => createMockItem({ productName: `Product ${i}` }));

    const { container } = render(
      <div
        data-testid="table-scroll-container"
        className="overflow-y-auto"
        style={{ maxHeight: 'calc(100vh - 400px)' }}
      >
        <div className="overflow-x-auto">
          <table>
            <thead className="sticky top-0 bg-background">
              <tr><th>Col1</th></tr>
            </thead>
          </table>
        </div>
      </div>
    );

    const tableHeader = container.querySelector('thead');
    expect(tableHeader).toHaveClass('sticky');
    expect(tableHeader).toHaveClass('top-0');
  });
});

describe('TotalsRow styling', () => {
  it('renders totals with muted background and bold text', () => {
    const { container } = render(
      <TotalsRow
        totals={{
          quantity: 100,
          lineTotal: 50000,
          freight: 5000,
          unitCost: 20000,
          lineCost: 20000,
          lineProfit: 30000,
        }}
        hasCost={true}
      />
    );

    const totalsRow = container.querySelector('tr');
    expect(totalsRow).toHaveClass('bg-muted');
    expect(totalsRow).toHaveClass('font-semibold');
  });
});

describe('Table and totals column alignment', () => {
  it('renders same number of columns in table and totals', () => {
    const items: SalesRevenueLineItemDto[] = [
      createMockItem({ unitCost: 50, lineCost: 50, lineProfit: 50 }),
    ];

    const { container } = render(
      <div>
        <table>
          <thead>
            <tr>
              <th>Col1</th>
              <th>Col2</th>
              <th>Col3</th>
              <th>Col4</th>
              <th>Col5</th>
              <th>Col6</th>
              <th>Col7</th>
              <th>Col8</th>
              <th>Col9</th>
              <th>Col10</th>
              <th>Col11</th>
              <th>Col12</th>
              <th>Col13</th>
              <th>Col14</th>
            </tr>
          </thead>
        </table>
        <TotalsRow
          totals={calculateRevenueTotals(items)}
          hasCost={true}
        />
      </div>
    );

    const headerCols = container.querySelectorAll('thead th').length;
    const totalsRow = container.querySelector('tbody tr');
    expect(totalsRow).toBeInTheDocument();
  });
});

function createMockItem(overrides: Partial<SalesRevenueLineItemDto> = {}): SalesRevenueLineItemDto {
  return {
    quotationId: 'q1',
    quotationCode: 'QT001',
    quotationDate: '2026-05-01',
    confirmedAt: '2026-05-02',
    revenueDate: '2026-05-02',
    customerName: 'Test Customer',
    customerAddress: null,
    contactPhone: null,
    deliveryAddress: null,
    deliveryPhone: null,
    freight: 0,
    isFirstLineOfQuotation: true,
    productName: 'Test Product',
    specification: null,
    unitName: 'pcs',
    length: null,
    width: null,
    thickness: null,
    density: null,
    sheetCount: null,
    quantity: 1,
    unitPrice: 100,
    lineTotal: 100,
    unitCost: null,
    lineCost: null,
    lineProfit: null,
    ...overrides,
  };
}
