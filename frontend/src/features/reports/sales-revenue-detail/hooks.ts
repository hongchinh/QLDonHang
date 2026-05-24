import { useQuery } from '@tanstack/react-query';
import { salesRevenueDetailApi } from './api';
import { salesRevenueDetailKeys } from './keys';
import type { SalesRevenueLineItemsParams } from './types';

export function useSalesRevenueDetail(
  saleUserId: string | undefined,
  params: SalesRevenueLineItemsParams,
  enabled = true,
) {
  return useQuery({
    queryKey: salesRevenueDetailKeys.lines(saleUserId ?? '', params),
    queryFn: () => salesRevenueDetailApi.getLines(saleUserId!, params),
    enabled: enabled && !!saleUserId && !!params.from && !!params.to,
    staleTime: 5 * 60 * 1000,
  });
}

export function useRevenueLineItems(params: SalesRevenueLineItemsParams, enabled = true) {
  return useQuery({
    queryKey: salesRevenueDetailKeys.revenueLines(params),
    queryFn: () => salesRevenueDetailApi.getRevenueLines(params),
    enabled: enabled && !!params.from && !!params.to,
    staleTime: 30_000,
  });
}
