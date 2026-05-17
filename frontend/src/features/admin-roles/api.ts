import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  CreateRolePayload,
  PermissionDto,
  RoleDetail,
  RoleListItem,
  UpdateRolePayload,
  UpdateRolePermissionsPayload,
} from './types';

export const adminRolesApi = {
  listPermissions: () => apiGet<PermissionDto[]>('/admin/permissions'),
  list: () => apiGet<RoleListItem[]>('/admin/roles'),
  getDetail: (id: string) => apiGet<RoleDetail>(`/admin/roles/${id}`),
  create: (payload: CreateRolePayload) => apiPost<RoleDetail>('/admin/roles', payload),
  update: (id: string, payload: UpdateRolePayload) =>
    apiPut<RoleDetail>(`/admin/roles/${id}`, payload),
  updatePermissions: (id: string, payload: UpdateRolePermissionsPayload) =>
    apiPut<RoleDetail>(`/admin/roles/${id}/permissions`, payload),
  remove: (id: string) => apiDelete<void>(`/admin/roles/${id}`),
};
