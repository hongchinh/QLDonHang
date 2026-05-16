import type { SalesRevenueReportParams } from './types';

export const salesRevenueKeys = {
  all: ['reports', 'sales-revenue'] as const,
  list: (p: SalesRevenueReportParams) => ['reports', 'sales-revenue', p] as const,
};
