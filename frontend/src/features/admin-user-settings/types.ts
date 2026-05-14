import type { MyQuotationSettings, LockAtStatus } from '@/features/me-settings/types';

export type { LockAtStatus };
export type UserSettings = MyQuotationSettings;

export interface UpdateLockAtRequest {
  lockAtStatus: LockAtStatus;
}

export interface BulkTransferRequest {
  toUserId: string;
  includeCancelled?: boolean;
  reason?: string;
}

export interface BulkTransferResult {
  affectedCount: number;
  fromUserId: string;
  toUserId: string;
}
