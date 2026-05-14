import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { quotationsApi } from './api';
import { quotationKeys } from './keys';
import type {
  QuotationAction,
  QuotationListParams,
  TransferOwnerRequest,
  UpsertQuotationRequest,
} from './types';

export function useQuotations(params: QuotationListParams) {
  return useQuery({
    queryKey: quotationKeys.list(params),
    queryFn: () => quotationsApi.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useQuotation(id: string | undefined) {
  return useQuery({
    queryKey: quotationKeys.detail(id ?? ''),
    queryFn: () => quotationsApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateQuotation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertQuotationRequest) => quotationsApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: quotationKeys.lists() }),
  });
}

export function useUpdateQuotation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpsertQuotationRequest }) =>
      quotationsApi.update(id, data),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: quotationKeys.lists() });
      qc.invalidateQueries({ queryKey: quotationKeys.detail(id) });
    },
  });
}

export function useDeleteQuotation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotationsApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: quotationKeys.lists() }),
  });
}

export function useTransitionQuotation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, action }: { id: string; action: QuotationAction }) =>
      quotationsApi.transition(id, action),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: quotationKeys.lists() });
      qc.invalidateQueries({ queryKey: quotationKeys.detail(id) });
    },
  });
}

export function useTransferOwner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: TransferOwnerRequest }) =>
      quotationsApi.transferOwner(id, data),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: quotationKeys.lists() });
      qc.invalidateQueries({ queryKey: quotationKeys.detail(id) });
    },
  });
}

export function useCloneQuotation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotationsApi.clone(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: quotationKeys.lists() }),
  });
}
