import type { PagedResult } from '@/features/customers/types';
import type { PricingMode } from '@/features/products/types';

export type QuotationStatus = 'Draft' | 'Sent' | 'Confirmed' | 'Cancelled';
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
  ownerUserId: string;
  ownerFullName?: string;
  isOwnerDeleted: boolean;
  canEdit: boolean;
  canClone: boolean;
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
  // Cost/profit are nullable: server redacts them when caller lacks `quotations.view_cost`.
  totalCost?: number;
  grossProfit?: number;
  status: QuotationStatus;
  confirmedAt?: string;
  confirmedByUserId?: string;
  confirmedByName?: string;
  cancelledAt?: string;
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
  subtotal: number;
  discount: number;
  freight: number;
  total: number;
  status: QuotationStatus;
  confirmedAt?: string;
  ownerUserId: string;
  ownerFullName?: string;
  isOwnerDeleted: boolean;
  canClone: boolean;
  createdByName?: string;
  createdAt: string;
}

export interface QuotationListAggregates {
  subtotal: number;
  discount: number;
  freight: number;
  total: number;
}

export interface QuotationOwnerOption {
  id: string;
  fullName: string;
  isDeleted: boolean;
  quotationCount: number;
}

export interface QuotationListResult extends PagedResult<QuotationListItem> {
  aggregates: QuotationListAggregates;
}

export interface TransferOwnerRequest {
  newOwnerUserId: string;
  reason?: string;
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
  statuses?: QuotationStatus[];
  customerId?: string;
  from?: string;
  to?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  ownerUserIds?: string[];
}

export type { PagedResult };
