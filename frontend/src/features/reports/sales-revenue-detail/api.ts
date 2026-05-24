import { apiGet } from '@/lib/api-client';
import type { SalesRevenueLineItemDto, SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailApi = {
  getLines: (saleUserId: string, params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>(`/reports/sales-revenue/${saleUserId}/lines`, params),
  getRevenueLines: (params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>('/reports/revenue-lines', params),
};
