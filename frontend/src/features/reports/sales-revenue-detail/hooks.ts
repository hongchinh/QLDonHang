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
    enabled: enabled && !!saleUserId,
    staleTime: 5 * 60 * 1000,
  });
}
