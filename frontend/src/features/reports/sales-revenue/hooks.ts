import { useQuery } from '@tanstack/react-query';
import { salesRevenueApi } from './api';
import { salesRevenueKeys } from './keys';
import type { SalesRevenueReportParams } from './types';

export function useSalesRevenue(params: SalesRevenueReportParams, enabled = true) {
  return useQuery({
    queryKey: salesRevenueKeys.list(params),
    queryFn: () => salesRevenueApi.get(params),
    enabled,
  });
}
