import api, { apiDelete, apiGet, apiPatch, apiPost, apiPut } from '@/lib/api-client';
import type {
  PagedResult,
  Quotation,
  QuotationAction,
  QuotationListItem,
  QuotationListParams,
  TransferOwnerRequest,
  UpsertQuotationRequest,
} from './types';

export const quotationsApi = {
  list: (params: QuotationListParams) =>
    apiGet<PagedResult<QuotationListItem>>('/quotations', params),
  get: (id: string) => apiGet<Quotation>(`/quotations/${id}`),
  create: (data: UpsertQuotationRequest) => apiPost<Quotation>('/quotations', data),
  update: (id: string, data: UpsertQuotationRequest) =>
    apiPut<Quotation>(`/quotations/${id}`, data),
  remove: (id: string) => apiDelete(`/quotations/${id}`),
  transition: (id: string, action: QuotationAction) =>
    apiPost<Quotation>(`/quotations/${id}/transition`, { action }),
  transferOwner: (id: string, data: TransferOwnerRequest) =>
    apiPatch<Quotation>(`/quotations/${id}/owner`, data),
  clone: (id: string) => apiPost<Quotation>(`/quotations/${id}/clone`, {}),
  downloadPdf: async (id: string): Promise<Blob> => {
    const res = await api.get(`/quotations/${id}/pdf`, { responseType: 'blob' });
    return res.data as Blob;
  },
  downloadExcel: async (id: string): Promise<Blob> => {
    const res = await api.get(`/quotations/${id}/excel`, { responseType: 'blob' });
    return res.data as Blob;
  },
};
