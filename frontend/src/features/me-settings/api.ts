import api, { apiDelete, apiGet, type ApiResponse } from '@/lib/api-client';
import type { MyQuotationSettings } from './types';

export type HandoverTemplateType = 'handover-with-price' | 'handover-no-price';

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

  uploadHandoverTemplate: async (
    file: File,
    type: HandoverTemplateType,
  ): Promise<MyQuotationSettings> => {
    const form = new FormData();
    form.append('file', file);
    const res = await api.put<ApiResponse<MyQuotationSettings>>(
      `/me/quotation-settings/template?type=${type}`,
      form,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    if (!res.data.success || !res.data.data) {
      throw new Error(res.data.error?.message ?? 'Upload thất bại');
    }
    return res.data.data;
  },

  deleteHandoverTemplate: (type: HandoverTemplateType) =>
    apiDelete<MyQuotationSettings>(`/me/quotation-settings/template?type=${type}`),

  downloadHandoverTemplate: async (type: HandoverTemplateType): Promise<Blob> => {
    const res = await api.get(`/me/quotation-settings/template?type=${type}`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },

  downloadEffectiveTemplate: async (type: 'quotation' | HandoverTemplateType): Promise<Blob> => {
    const res = await api.get(`/me/quotation-settings/effective-template?type=${type}`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },

  downloadDefaultTemplate: async (type: 'quotation' | HandoverTemplateType): Promise<Blob> => {
    const res = await api.get(`/me/quotation-settings/default-template?type=${type}`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },
};
