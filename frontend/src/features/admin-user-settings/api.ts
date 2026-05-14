import { apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  BulkTransferRequest,
  BulkTransferResult,
  UpdateLockAtRequest,
  UserSettings,
} from './types';

export const adminUserSettingsApi = {
  getForUser: (userId: string) =>
    apiGet<UserSettings>(`/admin/user-settings/${userId}`),
  setLockAt: (userId: string, data: UpdateLockAtRequest) =>
    apiPut<UserSettings>(`/admin/user-settings/${userId}/lock-at`, data),
  bulkTransfer: (fromUserId: string, data: BulkTransferRequest) =>
    apiPost<BulkTransferResult>(`/admin/users/${fromUserId}/transfer-quotations`, data),
};
