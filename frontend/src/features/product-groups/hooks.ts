import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { lookupKeys } from '@/features/products/keys';
import { productGroupsApi } from './api';
import { productGroupKeys } from './keys';
import type {
  CreateProductGroupRequest,
  ProductGroupListParams,
  UpdateProductGroupRequest,
} from './types';

export function useProductGroups(params: ProductGroupListParams) {
  return useQuery({
    queryKey: productGroupKeys.list(params),
    queryFn: () => productGroupsApi.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useProductGroup(id: string | undefined) {
  return useQuery({
    queryKey: productGroupKeys.detail(id ?? ''),
    queryFn: () => productGroupsApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateProductGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateProductGroupRequest) => productGroupsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: productGroupKeys.lists() });
      qc.invalidateQueries({ queryKey: lookupKeys.productGroups() });
    },
  });
}

export function useUpdateProductGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProductGroupRequest }) =>
      productGroupsApi.update(id, data),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: productGroupKeys.lists() });
      qc.invalidateQueries({ queryKey: productGroupKeys.detail(id) });
      qc.invalidateQueries({ queryKey: lookupKeys.productGroups() });
    },
  });
}

export function useDeleteProductGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => productGroupsApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: productGroupKeys.lists() });
      qc.invalidateQueries({ queryKey: lookupKeys.productGroups() });
    },
  });
}
