import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminUsersApi } from './api';
import { adminUsersKeys } from './keys';
import type {
  AdminUserListParams,
  CreateUserPayload,
  ResetPasswordPayload,
  SetUserStatusPayload,
  UpdateUserPayload,
} from './types';

export function useAdminUsers(params: AdminUserListParams) {
  return useQuery({
    queryKey: adminUsersKeys.list(params),
    queryFn: () => adminUsersApi.list(params),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });
}

export function useAdminUserDetail(id: string | undefined) {
  return useQuery({
    queryKey: adminUsersKeys.detail(id ?? ''),
    queryFn: () => adminUsersApi.getDetail(id!),
    enabled: !!id,
  });
}

export function useCreateAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateUserPayload) => adminUsersApi.create(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminUsersKeys.lists() });
    },
  });
}

export function useUpdateAdminUser(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateUserPayload) => adminUsersApi.update(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminUsersKeys.lists() });
      qc.invalidateQueries({ queryKey: adminUsersKeys.detail(id) });
    },
  });
}

export function useResetAdminUserPassword(id: string) {
  return useMutation({
    mutationFn: (payload: ResetPasswordPayload) => adminUsersApi.resetPassword(id, payload),
  });
}

export function useSetAdminUserStatus(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: SetUserStatusPayload) => adminUsersApi.setStatus(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminUsersKeys.lists() });
      qc.invalidateQueries({ queryKey: adminUsersKeys.detail(id) });
    },
  });
}

export function useDeleteAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => adminUsersApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminUsersKeys.lists() });
    },
  });
}
