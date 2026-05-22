import api, { apiDelete, apiGet, apiPatch, apiPost, apiPut } from '@/lib/api-client';
import type {
  Quotation,
  QuotationAction,
  QuotationActivity,
  QuotationListParams,
  QuotationListResult,
  QuotationOwnerOption,
  TransferOwnerRequest,
  UpsertQuotationRequest,
} from './types';

export const quotationsApi = {
  list: (params: QuotationListParams) => {
    const { statuses, ownerUserIds, ...rest } = params;
    const serialized: Record<string, unknown> = { ...rest };
    if (statuses && statuses.length > 0) {
      serialized.status = statuses.join(',');
    }
    if (ownerUserIds && ownerUserIds.length > 0) {
      serialized.ownerUserIds = ownerUserIds.join(',');
    }
    return apiGet<QuotationListResult>('/quotations', serialized);
  },
  listOwners: (includeDeleted = true) =>
    apiGet<QuotationOwnerOption[]>('/quotations/owners', { includeDeleted }),
  get: (id: string) => apiGet<Quotation>(`/quotations/${id}`),
  listActivities: (id: string) => apiGet<QuotationActivity[]>(`/quotations/${id}/activities`),
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
  downloadHandoverWithPriceExcel: async (id: string): Promise<Blob> => {
    const res = await api.get(`/quotations/${id}/handover-with-price/excel`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },
  downloadHandoverWithPricePdf: async (id: string): Promise<Blob> => {
    const res = await api.get(`/quotations/${id}/handover-with-price/pdf`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },
  downloadHandoverNoPriceExcel: async (id: string): Promise<Blob> => {
    const res = await api.get(`/quotations/${id}/handover-no-price/excel`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },
  downloadHandoverNoPricePdf: async (id: string): Promise<Blob> => {
    const res = await api.get(`/quotations/${id}/handover-no-price/pdf`, {
      responseType: 'blob',
    });
    return res.data as Blob;
  },
};
