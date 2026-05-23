import type { SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailKeys = {
  lines: (saleUserId: string, p: SalesRevenueLineItemsParams) =>
    ['reports', 'sales-revenue-detail', saleUserId, p] as const,
};
