import type { SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailKeys = {
  all: ['reports', 'sales-revenue-detail'] as const,
  lines: (saleUserId: string, p: SalesRevenueLineItemsParams) =>
    ['reports', 'sales-revenue-detail', saleUserId, p] as const,
};
