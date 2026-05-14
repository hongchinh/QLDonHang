import api, { apiDelete, apiGet, type ApiResponse } from '@/lib/api-client';
import type { MyQuotationSettings } from './types';

export const meSettingsApi = {
  getMine: () => apiGet<MyQuotationSettings>('/me/quotation-settings'),

  uploadTemplate: async (file: File): Promise<MyQuotationSettings> => {
    const form = new FormData();
    form.append('file', file);
    const res = await api.put<ApiResponse<MyQuotationSettings>>(
      '/me/quotation-settings/template',
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    if (!res.data.success || !res.data.data) {
      throw new Error(res.data.error?.message ?? 'Upload thất bại');
    }
    return res.data.data;
  },

  deleteTemplate: () => apiDelete<MyQuotationSettings>('/me/quotation-settings/template'),

  downloadTemplate: async (): Promise<Blob> => {
    const res = await api.get('/me/quotation-settings/template', { responseType: 'blob' });
    return res.data as Blob;
  },
};
