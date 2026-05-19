import { useQuery } from '@tanstack/react-query';
import { vehicleRevenueApi } from './api';
import { vehicleRevenueKeys } from './keys';
import type { VehicleRevenueReportParams } from './types';

export function useVehicleRevenue(params: VehicleRevenueReportParams, enabled = true) {
  return useQuery({
    queryKey: vehicleRevenueKeys.list(params),
    queryFn: () => vehicleRevenueApi.get(params),
    enabled,
  });
}
