import type { ProductGroupListParams } from './types';

export const productGroupKeys = {
  all: ['product-groups'] as const,
  lists: () => [...productGroupKeys.all, 'list'] as const,
  list: (params: ProductGroupListParams) => [...productGroupKeys.lists(), params] as const,
  details: () => [...productGroupKeys.all, 'detail'] as const,
  detail: (id: string) => [...productGroupKeys.details(), id] as const,
};
