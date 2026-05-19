import { describe, expect, it } from 'vitest';
import {
  computeLineCost,
  computeLineTotal,
  computeTotals,
} from './compute-line';

describe('compute-line', () => {
  it('PerUnit line total: qty x unitPrice', () => {
    expect(computeLineTotal({
      pricingMode: 'PerUnit',
      quantity: 5,
      unitPrice: 12000,
    })).toBe(60000);
  });

  it('PerLinearMeter line total: length x sheetCount x unitPrice', () => {
    expect(computeLineTotal({
      pricingMode: 'PerLinearMeter',
      length: 2500,
      sheetCount: 4,
      quantity: 1,
      unitPrice: 25000,
    })).toBe(250000);
  });

  it('PerSquareMeter line total: length x width x sheetCount x unitPrice', () => {
    expect(computeLineTotal({
      pricingMode: 'PerSquareMeter',
      length: 2000,
      width: 1000,
      sheetCount: 2,
      quantity: 1,
      unitPrice: 50000,
    })).toBe(200000);
  });

  it('PerCubicMeter line total: length x width x thickness x sheetCount x unitPrice', () => {
    expect(computeLineTotal({
      pricingMode: 'PerCubicMeter',
      length: 1000,
      width: 1000,
      thickness: 500,
      sheetCount: 1,
      quantity: 1,
      unitPrice: 1000000,
    })).toBe(500000);
  });

  it('returns zero total when required dimensions are missing', () => {
    expect(
      computeLineTotal({
        pricingMode: 'PerSquareMeter',
        length: 2000,
        quantity: 1,
        unitPrice: 50000,
      }),
    ).toBe(0);
  });

  it('rounds line total and line cost to integer VND', () => {
    const line = {
      pricingMode: 'PerLinearMeter' as const,
      length: 333,
      sheetCount: 1,
      quantity: 1,
      unitPrice: 1000,
      unitCost: 500,
    };
    expect(computeLineTotal(line)).toBe(333);
    expect(computeLineCost(line)).toBe(167);
  });

  it('totals: subtotal + tax - discount + freight', () => {
    const totals = computeTotals(
      [
        { pricingMode: 'PerUnit', quantity: 5, unitPrice: 12000 },
        { pricingMode: 'PerUnit', quantity: 1, unitPrice: 40000 },
      ],
      { taxRate: 10, discount: 10000, freight: 5000 },
    );
    expect(totals.subtotal).toBe(100000);
    expect(totals.taxAmount).toBe(10000);
    expect(totals.total).toBe(105000);
  });

  it('totals: gross profit excludes freight, includes discount', () => {
    const totals = computeTotals(
      [{ pricingMode: 'PerUnit', quantity: 5, unitPrice: 12000, unitCost: 8000 }],
      { taxRate: 0, discount: 10000, freight: 50000 },
    );
    expect(totals.subtotal).toBe(60000);
    expect(totals.totalCost).toBe(40000);
    expect(totals.grossProfit).toBe(10000);
  });

  it('tax rounds to integer VND', () => {
    const totals = computeTotals(
      [{ pricingMode: 'PerUnit', quantity: 1, unitPrice: 123 }],
      { taxRate: 8, discount: 0, freight: 0 },
    );
    expect(totals.taxAmount).toBe(10);
  });

  it('line cost / profit', () => {
    const line = {
      pricingMode: 'PerUnit' as const,
      quantity: 5,
      unitPrice: 12000,
      unitCost: 8000,
    };
    expect(computeLineCost(line)).toBe(40000);
    expect(computeLineTotal(line) - (computeLineCost(line) ?? 0)).toBe(20000);
  });
});
