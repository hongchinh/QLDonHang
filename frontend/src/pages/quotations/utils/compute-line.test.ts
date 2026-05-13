import { describe, expect, it } from 'vitest';
import {
  computeLineCost,
  computeLineTotal,
  computeTotals,
  deriveQuantityFromDimensions,
} from './compute-line';

describe('compute-line', () => {
  it('PerUnit line total: qty × unitPrice', () => {
    expect(computeLineTotal({
      pricingMode: 'PerUnit',
      quantity: 5,
      unitPrice: 12000,
    })).toBe(60000);
  });

  it('PerSquareMeter derives area from L*W*Sheet', () => {
    const area = deriveQuantityFromDimensions({
      pricingMode: 'PerSquareMeter',
      length: 2000,
      width: 1000,
      sheetCount: 3,
      quantity: 0,
      unitPrice: 0,
    });
    expect(area).toBe(6);
  });

  it('PerLinearMeter derives meters from L*Sheet/1000', () => {
    const m = deriveQuantityFromDimensions({
      pricingMode: 'PerLinearMeter',
      length: 2500,
      sheetCount: 4,
      quantity: 0,
      unitPrice: 0,
    });
    expect(m).toBe(10);
  });

  it('PerCubicMeter derives m³ from L*W*T*Sheet/1e9', () => {
    const v = deriveQuantityFromDimensions({
      pricingMode: 'PerCubicMeter',
      length: 1000,
      width: 1000,
      thickness: 500,
      sheetCount: 1,
      quantity: 0,
      unitPrice: 0,
    });
    expect(v).toBe(0.5);
  });

  it('returns undefined when dimensions missing', () => {
    expect(
      deriveQuantityFromDimensions({
        pricingMode: 'PerSquareMeter',
        length: 2000,
        sheetCount: 1,
        quantity: 0,
        unitPrice: 0,
      }),
    ).toBeUndefined();
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
