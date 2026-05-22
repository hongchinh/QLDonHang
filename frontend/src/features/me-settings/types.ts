export type LockAtStatus = 'Sent' | 'Confirmed' | null;

export interface MyQuotationSettings {
  userId: string;
  userFullName?: string;
  lockAtStatus: LockAtStatus;
  templateFileName: string | null;
  templateOriginalName: string | null;
  templateUploadedAt: string | null;
  handoverWithPriceTemplateFileName: string | null;
  handoverWithPriceTemplateOriginalName: string | null;
  handoverWithPriceTemplateUploadedAt: string | null;
  handoverNoPriceTemplateFileName: string | null;
  handoverNoPriceTemplateOriginalName: string | null;
  handoverNoPriceTemplateUploadedAt: string | null;
}
