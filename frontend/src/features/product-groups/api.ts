import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  CreateProductGroupRequest,
  PagedResult,
  ProductGroup,
  ProductGroupListItem,
  ProductGroupListParams,
  UpdateProductGroupRequest,
} from './types';

export const productGroupsApi = {
  list: (params: ProductGroupListParams) =>
    apiGet<PagedResult<ProductGroupListItem>>('/product-groups', params),
  get: (id: string) => apiGet<ProductGroup>(`/product-groups/${id}`),
  create: (data: CreateProductGroupRequest) =>
    apiPost<ProductGroup>('/product-groups', data),
  update: (id: string, data: UpdateProductGroupRequest) =>
    apiPut<ProductGroup>(`/product-groups/${id}`, data),
  remove: (id: string) => apiDelete(`/product-groups/${id}`),
};
