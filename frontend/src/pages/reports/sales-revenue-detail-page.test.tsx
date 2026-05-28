import { describe, expect, it } from 'vitest';
import type { SalesRevenueLineItemDto } from '@/features/reports/sales-revenue-detail/types';
import { calculateRevenueTotals } from './sales-revenue-detail-page';

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
