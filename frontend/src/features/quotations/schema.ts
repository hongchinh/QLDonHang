import { z } from 'zod';
import { optionalNumber, optionalString } from '@/lib/zod-helpers';

const quotationLineSchema = z.object({
  // Client-only stable key for React reconciliation. useFieldArray regenerates
  // field.id on every nested setValue (RHF v7 fires _subjects.array.next on any
  // leaf change within an array, which causes the keyName ref to be remapped
  // with fresh UUIDs). Keying <tr> on field.id therefore unmounts the row
  // every keystroke and steals focus. _uiKey is generated once at creation
  // (server id for existing lines, fresh uuid for new ones), preserved in form
  // state, and stripped before submitting.
  _uiKey: z.string().optional(),
  id: z.string().uuid().optional(),
  sortOrder: z.number().int().nonnegative(),
  productId: z.string().uuid().optional(),
  productCode: optionalString(50),
  productName: z.string().min(1, 'Tên hàng là bắt buộc').max(1000),
  specification: optionalString(500),
  unitName: z.string().min(1, 'ĐVT là bắt buộc').max(100),
  pricingMode: z.enum(['PerUnit', 'PerSquareMeter', 'PerLinearMeter', 'PerCubicMeter']),
  length: optionalNumber({ min: 0 }),
  width: optionalNumber({ min: 0 }),
  thickness: optionalNumber({ min: 0 }),
  density: optionalNumber({ min: 0 }),
  sheetCount: optionalNumber({ min: 0 }),
  quantity: z.coerce.number().nonnegative(),
  unitPrice: z.coerce.number().nonnegative(),
  lineTotal: optionalNumber({ min: 0 }),
  unitCost: optionalNumber(),
  lineCost: optionalNumber(),
  lineProfit: optionalNumber(),
  note: optionalString(1000),
}).superRefine((line, ctx) => {
  const L = line.length ?? 0;
  const W = line.width ?? 0;
  const T = line.thickness ?? 0;
  const sheets = line.sheetCount ?? 0;
  let effectiveQty: number;
  switch (line.pricingMode) {
    case 'PerSquareMeter': effectiveQty = (L * W * sheets) / 1_000_000; break;
    case 'PerLinearMeter': effectiveQty = (L * sheets) / 1_000; break;
    case 'PerCubicMeter': effectiveQty = (L * W * T * sheets) / 1_000_000_000; break;
    default: effectiveQty = line.quantity;
  }
  if (effectiveQty <= 0) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Số lượng phải > 0', path: ['quantity'] });
  }
});

export const quotationSchema = z.object({
  customerId: z.string().uuid('Chọn khách hàng'),
  customerName: optionalString(255),
  quotationDate: z.string().min(1, 'Chọn ngày báo giá'),
  deliveryAddress: optionalString(1000),
  deliveryRecipient: optionalString(255),
  deliveryPhone: optionalString(30),
  transportVehicleNumber: optionalString(50),
  deliveryDate: optionalString(20),
  deliveryNote: optionalString(1000),
  taxRate: z.coerce.number().min(0).max(100),
  discount: z.coerce.number().min(0),
  freight: z.coerce.number().min(0),
  advancePayment: z.coerce.number().min(0),
  internalNote: optionalString(2000),
  lines: z.array(quotationLineSchema).min(1, 'Báo giá phải có ít nhất 1 dòng'),
});

export type QuotationFormValues = z.input<typeof quotationSchema>;
export type QuotationFormParsed = z.output<typeof quotationSchema>;
export type QuotationLineFormValues = z.input<typeof quotationLineSchema>;
export type QuotationLineFormParsed = z.output<typeof quotationLineSchema>;
