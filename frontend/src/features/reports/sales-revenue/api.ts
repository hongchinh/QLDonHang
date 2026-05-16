import { apiGet } from '@/lib/api-client';
import type { SalesRevenueReport, SalesRevenueReportParams } from './types';

export const salesRevenueApi = {
  get: (params: SalesRevenueReportParams) =>
    apiGet<SalesRevenueReport>('/reports/sales-revenue', params),
};
