import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { quotationSettingsApi } from './api';
import type { UpdateQuotationSystemSettingsRequest } from './types';

const KEYS = { settings: ['quotation-settings'] as const };

export function useQuotationSystemSettings() {
  return useQuery({ queryKey: KEYS.settings, queryFn: quotationSettingsApi.get });
}

export function useUpdateQuotationSystemSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateQuotationSystemSettingsRequest) =>
      quotationSettingsApi.update(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.settings }),
  });
}
