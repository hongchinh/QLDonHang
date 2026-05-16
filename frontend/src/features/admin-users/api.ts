import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  AdminUserDetail,
  AdminUserListItem,
  AdminUserListParams,
  CreateUserPayload,
  ResetPasswordPayload,
  SetUserStatusPayload,
  UpdateUserPayload,
} from './types';

export const adminUsersApi = {
  list: (params: AdminUserListParams) =>
    apiGet<AdminUserListItem[]>('/admin/users', params),
  getDetail: (id: string) => apiGet<AdminUserDetail>(`/admin/users/${id}`),
  create: (payload: CreateUserPayload) => apiPost<AdminUserDetail>('/admin/users', payload),
  update: (id: string, payload: UpdateUserPayload) =>
    apiPut<AdminUserDetail>(`/admin/users/${id}`, payload),
  resetPassword: (id: string, payload: ResetPasswordPayload) =>
    apiPost<void>(`/admin/users/${id}/reset-password`, payload),
  setStatus: (id: string, payload: SetUserStatusPayload) =>
    apiPost<void>(`/admin/users/${id}/status`, payload),
  remove: (id: string) => apiDelete<void>(`/admin/users/${id}`),
};
