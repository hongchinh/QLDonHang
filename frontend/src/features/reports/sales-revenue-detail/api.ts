import api, { apiGet } from '@/lib/api-client';
import type { SalesRevenueLineItemDto, SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailApi = {
  getLines: (saleUserId: string, params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>(`/reports/sales-revenue/${saleUserId}/lines`, params),
  getRevenueLines: (params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>('/reports/revenue-lines', params),
  downloadRevenueExcel: async (params: SalesRevenueLineItemsParams): Promise<void> => {
    const res = await api.get('/reports/revenue-lines/excel', {
      params,
      responseType: 'blob',
    });
    const url = URL.createObjectURL(res.data as Blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `BaoCaoDoanhThu_${params.from.replace(/-/g, '')}_${params.to.replace(/-/g, '')}.xlsx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },
};
