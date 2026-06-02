export interface SalesRevenueLineItemDto {
  quotationId: string;
  quotationCode: string;
  quotationDate: string;       // DateOnly → "YYYY-MM-DD"
  confirmedAt: string | null;  // DateTime? → ISO string
  revenueDate: string | null;  // effective revenue date per system settings
  customerName: string;
  customerAddress: string | null;
  contactPhone: string | null;
  deliveryAddress: string | null;
  deliveryPhone: string | null;
  freight: number;
  taxAmount: number;
  total: number;
  isFirstLineOfQuotation: boolean;
  productName: string;
  specification: string | null;
  unitName: string;
  length: number | null;
  width: number | null;
  thickness: number | null;
  density: number | null;
  sheetCount: number | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  unitCost: number | null;
  lineCost: number | null;
  lineProfit: number | null;
}

export interface SalesRevenueLineItemsParams {
  from: string;
  to: string;
  saleUserId?: string;
}
