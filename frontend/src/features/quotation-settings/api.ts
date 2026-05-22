import { apiGet, apiPut } from '@/lib/api-client';
import type { QuotationSystemSettings, UpdateQuotationSystemSettingsRequest } from './types';

export const quotationSettingsApi = {
  get: () => apiGet<QuotationSystemSettings>('/settings/quotation'),
  update: (data: UpdateQuotationSystemSettingsRequest) =>
    apiPut<QuotationSystemSettings>('/settings/quotation', data),
};
