import { apiGet } from '@/lib/api-client';
import type { AdminUserListItem, AdminUserListParams } from './types';

export const adminUsersApi = {
  list: (params: AdminUserListParams) =>
    apiGet<AdminUserListItem[]>('/admin/users', params),
};
