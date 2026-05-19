import type { QuotationListParams } from './types';

export const quotationKeys = {
  all: ['quotations'] as const,
  lists: () => [...quotationKeys.all, 'list'] as const,
  list: (params: QuotationListParams) => [...quotationKeys.lists(), params] as const,
  details: () => [...quotationKeys.all, 'detail'] as const,
  detail: (id: string) => [...quotationKeys.details(), id] as const,
  activities: (id: string) => [...quotationKeys.detail(id), 'activities'] as const,
  owners: (includeDeleted: boolean) =>
    [...quotationKeys.all, 'owners', { includeDeleted }] as const,
};
