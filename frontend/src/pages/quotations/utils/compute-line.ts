import type { PricingMode } from '@/features/products/types';

export interface LineLike {
  pricingMode: PricingMode;
  length?: number;
  width?: number;
  thickness?: number;
  density?: number;
  sheetCount?: number;
  quantity: number;
  unitPrice: number;
  unitCost?: number;
}

export interface HeaderLike {
  taxRate: number;
  discount: number;
  freight: number;
}

export function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

export function round0(value: number): number {
  return Math.round(value + Number.EPSILON);
}

// Derive default quantity from snapshot dimensions when those dimensions cover the
// requested pricing mode. Returns undefined when the user hasn't supplied enough info.
// (Backend always trusts `Quantity` from the request; this is FE convenience only.)
export function deriveQuantityFromDimensions(line: LineLike): number | undefined {
  const sheet = line.sheetCount ?? 1;
  const L = line.length;
  const W = line.width;
  const T = line.thickness;
  switch (line.pricingMode) {
    case 'PerSquareMeter':
      if (L === undefined || W === undefined) return undefined;
      return round2((L * W * sheet) / 1_000_000);
    case 'PerLinearMeter':
      if (L === undefined) return undefined;
      return round2((L * sheet) / 1000);
    case 'PerCubicMeter':
      if (L === undefined || W === undefined || T === undefined) return undefined;
      return round2((L * W * T * sheet) / 1_000_000_000);
    case 'PerUnit':
    default:
      return undefined;
  }
}

export function computeLineTotal(line: LineLike): number {
  return round2(line.quantity * line.unitPrice);
}

export function computeLineCost(line: LineLike): number | undefined {
  return line.unitCost != null ? round2(line.quantity * line.unitCost) : undefined;
}

export interface Totals {
  subtotal: number;
  totalCost: number;
  taxAmount: number;
  total: number;
  grossProfit: number;
}

export function computeTotals(lines: LineLike[], header: HeaderLike): Totals {
  const subtotal = lines.reduce((sum, l) => sum + computeLineTotal(l), 0);
  const totalCost = lines.reduce((sum, l) => sum + (computeLineCost(l) ?? 0), 0);
  const taxAmount = round0((subtotal * header.taxRate) / 100);
  const total = subtotal - header.discount + header.freight + taxAmount;
  const grossProfit = subtotal - totalCost - header.discount;
  return { subtotal, totalCost, taxAmount, total, grossProfit };
}
