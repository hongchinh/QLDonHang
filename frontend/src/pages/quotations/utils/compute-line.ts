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

export function computePricingFactor(line: LineLike): number {
  const L = line.length ?? 0;
  const W = line.width ?? 0;
  const T = line.thickness ?? 0;
  switch (line.pricingMode) {
    case 'PerSquareMeter':
      return (L * W) / 1_000_000;
    case 'PerLinearMeter':
      return L / 1000;
    case 'PerCubicMeter':
      return (L * W * T) / 1_000_000_000;
    case 'PerUnit':
    default:
      return 1;
  }
}

export function computeLineTotal(line: LineLike): number {
  return round2(line.quantity * computePricingFactor(line) * line.unitPrice);
}

export function computeLineCost(line: LineLike): number | undefined {
  return line.unitCost != null ? round2(line.quantity * computePricingFactor(line) * line.unitCost) : undefined;
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
