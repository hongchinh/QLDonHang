import { apiGet } from '@/lib/api-client';
import type { QuotationStats } from './types';

export const dashboardApi = {
  getQuotationStats: (params?: { from?: string; to?: string }) =>
    apiGet<QuotationStats>('/dashboard/quotation-stats', params),
};
