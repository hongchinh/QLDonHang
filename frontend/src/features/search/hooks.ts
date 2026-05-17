import { useQuery } from '@tanstack/react-query';
import { searchApi } from './api';

export const SEARCH_MIN_LENGTH = 3;

export function useGlobalSearch(query: string) {
  return useQuery({
    queryKey: ['search', 'global', query],
    queryFn: () => searchApi.global(query),
    enabled: query.length >= SEARCH_MIN_LENGTH,
    staleTime: 0,
    refetchOnWindowFocus: false,
  });
}
