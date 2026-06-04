import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  CreateProductRequest,
  LookupItem,
  PagedResult,
  Product,
  ProductListItem,
  ProductListParams,
  ProductSuggestion,
  UpdateProductRequest,
} from './types';

export const productsApi = {
  list: (params: ProductListParams) => apiGet<PagedResult<ProductListItem>>('/products', params),
  get: (id: string) => apiGet<Product>(`/products/${id}`),
  create: (data: CreateProductRequest) => apiPost<Product>('/products', data),
  update: (id: string, data: UpdateProductRequest) => apiPut<Product>(`/products/${id}`, data),
  remove: (id: string) => apiDelete(`/products/${id}`),
  search: (q: string, take = 20) =>
    apiGet<ProductSuggestion[]>('/products/search', { q, take }),
};

export const lookupsApi = {
  productGroups: () => apiGet<LookupItem[]>('/lookups/product-groups'),
  units: () => apiGet<LookupItem[]>('/lookups/units'),
  getOrCreateUnit: (name: string) => apiPost<LookupItem>('/lookups/units', { name }),
};
