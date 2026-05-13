import type { PagedResult } from '@/features/customers/types';
import type { PricingMode } from '@/features/products/types';

export type QuotationStatus = 'Draft' | 'Sent' | 'Confirmed' | 'ConvertedToOrder' | 'Cancelled';
export type QuotationAction = 'Send' | 'Confirm' | 'Cancel';

export interface QuotationLine {
  id: string;
  sortOrder: number;
  productId?: string;
  productCode?: string;
  productName: string;
  specification?: string;
  unitName: string;
  pricingMode: PricingMode;
  length?: number;
  width?: number;
  thickness?: number;
  density?: number;
  sheetCount?: number;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  unitCost?: number;
  lineCost?: number;
  lineProfit?: number;
  note?: string;
}

export interface Quotation {
  id: string;
  code: string;
  quotationDate: string;
  customerId: string;
  customerName: string;
  customerTaxCode?: string;
  customerAddress?: string;
  contactPerson?: string;
  contactPhone?: string;
  deliveryAddress?: string;
  deliveryRecipient?: string;
  deliveryPhone?: string;
  deliveryDate?: string;
  deliveryNote?: string;
  subtotal: number;
  discount: number;
  freight: number;
  taxRate: number;
  taxAmount: number;
  total: number;
  totalCost: number;
  grossProfit: number;
  status: QuotationStatus;
  internalNote?: string;
  lines: QuotationLine[];
  createdAt: string;
  createdBy?: string;
}

export interface QuotationListItem {
  id: string;
  code: string;
  quotationDate: string;
  customerName: string;
  contactPhone?: string;
  total: number;
  status: QuotationStatus;
  createdByName?: string;
  createdAt: string;
}

export interface UpsertQuotationLineRequest {
  id?: string;
  sortOrder: number;
  productId?: string;
  productCode?: string;
  productName: string;
  specification?: string;
  unitName: string;
  pricingMode: PricingMode;
  length?: number;
  width?: number;
  thickness?: number;
  density?: number;
  sheetCount?: number;
  quantity: number;
  unitPrice: number;
  unitCost?: number;
  note?: string;
}

export interface UpsertQuotationRequest {
  customerId: string;
  customerName?: string;
  quotationDate: string;
  deliveryAddress?: string;
  deliveryRecipient?: string;
  deliveryPhone?: string;
  deliveryDate?: string;
  deliveryNote?: string;
  taxRate: number;
  discount: number;
  freight: number;
  internalNote?: string;
  lines: UpsertQuotationLineRequest[];
}

export interface QuotationListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: QuotationStatus;
  customerId?: string;
  from?: string;
  to?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export type { PagedResult };
