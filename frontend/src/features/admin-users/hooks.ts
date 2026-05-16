import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { adminUsersApi } from './api';
import { adminUsersKeys } from './keys';
import type { AdminUserListParams } from './types';

export function useAdminUsers(params: AdminUserListParams) {
  return useQuery({
    queryKey: adminUsersKeys.list(params),
    queryFn: () => adminUsersApi.list(params),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });
}
