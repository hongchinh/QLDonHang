import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  Customer,
  CustomerListItem,
  CustomerListParams,
  CustomerSearchItem,
  CustomerSearchParams,
  PagedResult,
  UpsertCustomerRequest,
} from './types';

export const customersApi = {
  list: (params: CustomerListParams) =>
    apiGet<PagedResult<CustomerListItem>>('/customers', params),
  search: (params: CustomerSearchParams) =>
    apiGet<CustomerSearchItem[]>('/customers/search', {
      keyword: params.keyword,
      activeOnly: params.activeOnly ?? true,
      limit: params.limit ?? 20,
    }),
  get: (id: string) => apiGet<Customer>(`/customers/${id}`),
  create: (data: UpsertCustomerRequest) => apiPost<Customer>('/customers', data),
  update: (id: string, data: UpsertCustomerRequest) => apiPut<Customer>(`/customers/${id}`, data),
  remove: (id: string) => apiDelete(`/customers/${id}`),
};
