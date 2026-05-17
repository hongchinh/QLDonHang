import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminRolesApi } from './api';
import { adminRolesKeys } from './keys';
import type {
  CreateRolePayload,
  RoleDetail,
  UpdateRolePayload,
  UpdateRolePermissionsPayload,
} from './types';

export function usePermissionsCatalog() {
  return useQuery({
    queryKey: adminRolesKeys.permissionsCatalog,
    queryFn: () => adminRolesApi.listPermissions(),
    staleTime: 60_000,
  });
}

export function useAdminRoles() {
  return useQuery({
    queryKey: adminRolesKeys.lists(),
    queryFn: () => adminRolesApi.list(),
    staleTime: 60_000,
  });
}

export function useAdminRoleDetail(id: string | undefined) {
  return useQuery({
    queryKey: adminRolesKeys.detail(id ?? ''),
    queryFn: () => adminRolesApi.getDetail(id!),
    enabled: !!id,
  });
}

export function useAdminRoleDetails(ids: string[]) {
  return useQueries({
    queries: ids.map((id) => ({
      queryKey: adminRolesKeys.detail(id),
      queryFn: () => adminRolesApi.getDetail(id),
      staleTime: 60_000,
    })),
    combine: (results) => ({
      data: results.map((r) => r.data).filter((d): d is RoleDetail => d != null),
      isLoading: results.some((r) => r.isLoading),
      isError: results.some((r) => r.isError),
    }),
  });
}

export function useCreateAdminRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateRolePayload) => adminRolesApi.create(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminRolesKeys.lists() });
    },
  });
}

export function useUpdateAdminRole(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateRolePayload) => adminRolesApi.update(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminRolesKeys.lists() });
      qc.invalidateQueries({ queryKey: adminRolesKeys.detail(id) });
    },
  });
}

export function useUpdateAdminRolePermissions(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateRolePermissionsPayload) =>
      adminRolesApi.updatePermissions(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminRolesKeys.lists() });
      qc.invalidateQueries({ queryKey: adminRolesKeys.detail(id) });
    },
  });
}

export function useDeleteAdminRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminRolesApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminRolesKeys.lists() });
    },
  });
}
