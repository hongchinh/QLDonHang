import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { adminUserSettingsApi } from './api';
import { quotationKeys } from '@/features/quotations/keys';
import type { BulkTransferRequest, UpdateLockAtRequest } from './types';

export const userSettingsKeys = {
  detail: (userId: string) => ['admin', 'user-settings', userId] as const,
};

export function useUserSettings(userId: string | undefined) {
  return useQuery({
    queryKey: userSettingsKeys.detail(userId ?? ''),
    queryFn: () => adminUserSettingsApi.getForUser(userId!),
    enabled: !!userId,
  });
}

export function useSetLockAt(userId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateLockAtRequest) => adminUserSettingsApi.setLockAt(userId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: userSettingsKeys.detail(userId) }),
  });
}

export function useBulkTransfer(fromUserId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: BulkTransferRequest) =>
      adminUserSettingsApi.bulkTransfer(fromUserId, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: quotationKeys.lists() }),
  });
}
