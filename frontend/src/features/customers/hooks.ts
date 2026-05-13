import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { customersApi } from './api';
import { customerKeys } from './keys';
import type { CustomerListParams, UpsertCustomerRequest } from './types';

export function useCustomers(params: CustomerListParams) {
  return useQuery({
    queryKey: customerKeys.list(params),
    queryFn: () => customersApi.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useCustomer(id: string | undefined) {
  return useQuery({
    queryKey: customerKeys.detail(id ?? ''),
    queryFn: () => customersApi.get(id!),
    enabled: !!id,
  });
}

export function useCustomersSearch(
  keyword: string,
  opts?: { activeOnly?: boolean; limit?: number },
) {
  const trimmed = keyword.trim();
  const params = { keyword: trimmed, activeOnly: opts?.activeOnly ?? true, limit: opts?.limit ?? 20 };
  return useQuery({
    queryKey: customerKeys.search(params),
    queryFn: () => customersApi.search(params),
    enabled: trimmed.length > 0,
    staleTime: 30_000,
    placeholderData: keepPreviousData,
  });
}

export function useCreateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertCustomerRequest) => customersApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: customerKeys.lists() }),
  });
}

export function useUpdateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpsertCustomerRequest }) =>
      customersApi.update(id, data),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: customerKeys.lists() });
      qc.invalidateQueries({ queryKey: customerKeys.detail(id) });
    },
  });
}

export function useDeleteCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => customersApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: customerKeys.lists() }),
  });
}
