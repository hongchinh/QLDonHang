import { apiGet } from '@/lib/api-client';
import type {
  ActivityItem,
  DashboardParams,
  DashboardSummary,
  Granularity,
  QuotationStats,
  RevenueSeries,
  SalesLeaderboardItem,
  TopCustomer,
  TopProduct,
} from './types';

export const dashboardApi = {
  getQuotationStats: (params?: { from?: string; to?: string }) =>
    apiGet<QuotationStats>('/dashboard/quotation-stats', params),

  getSummary: (p: { from?: string; to?: string; saleUserId?: string }) =>
    apiGet<DashboardSummary>('/dashboard/summary', p),

  getRevenueSeries: (p: DashboardParams & { granularity: Granularity }) =>
    apiGet<RevenueSeries>('/dashboard/revenue-series', p),

  getTopCustomers: (p: DashboardParams & { limit?: number }) =>
    apiGet<TopCustomer[]>('/dashboard/top-customers', p),

  getTopProducts: (p: DashboardParams & { limit?: number }) =>
    apiGet<TopProduct[]>('/dashboard/top-products', p),

  getRecentActivity: (p: { limit?: number }) =>
    apiGet<ActivityItem[]>('/dashboard/recent-activity', p),

  getLeaderboard: (p: { from: string; to: string; limit?: number }) =>
    apiGet<SalesLeaderboardItem[]>('/dashboard/sales-leaderboard', p),
};
