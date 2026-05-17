import type { GlobalSearchResult } from '@/features/search/api';

export function flattenResultIndex(
  data: GlobalSearchResult | undefined,
  activeIndex: number,
): { kind: 'customer' | 'quotation'; id: string } | null {
  if (!data) return null;
  const cs = data.customers;
  const qs = data.quotations;
  if (activeIndex < 0) return null;
  if (activeIndex < cs.length) return { kind: 'customer', id: cs[activeIndex].id };
  const qIdx = activeIndex - cs.length;
  if (qIdx < qs.length) return { kind: 'quotation', id: qs[qIdx].id };
  return null;
}

export function totalResultCount(data: GlobalSearchResult | undefined): number {
  if (!data) return 0;
  return data.customers.length + data.quotations.length;
}
