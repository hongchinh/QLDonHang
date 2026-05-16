export type LockAtStatus = 'Sent' | 'Confirmed' | null;

export interface MyQuotationSettings {
  userId: string;
  userFullName?: string;
  lockAtStatus: LockAtStatus;
  templateFileName: string | null;
  templateOriginalName: string | null;
  templateUploadedAt: string | null;
}
