import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from './api';

export function useQuotationStats(params?: { from?: string; to?: string }) {
  return useQuery({
    queryKey: ['dashboard', 'quotation-stats', params],
    queryFn: () => dashboardApi.getQuotationStats(params),
  });
}
