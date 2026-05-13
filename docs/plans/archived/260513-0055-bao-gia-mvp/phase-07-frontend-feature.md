# Phase 07 — Frontend feature module

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** S

## Objective
Build the data layer for `quotations` on the frontend — `types`, `api`, `hooks`, `keys`, `schema` — mirroring the conventions of `features/customers/` and `features/products/`. Also extend the products feature with the new typeahead search hook used by the line items grid.

## Files
- `frontend/src/features/quotations/types.ts` (new)
- `frontend/src/features/quotations/api.ts` (new)
- `frontend/src/features/quotations/hooks.ts` (new)
- `frontend/src/features/quotations/keys.ts` (new)
- `frontend/src/features/quotations/schema.ts` (new)
- `frontend/src/features/products/types.ts` (add `ProductSuggestion`)
- `frontend/src/features/products/api.ts` (add `productsApi.search`)
- `frontend/src/features/products/hooks.ts` (add `useProductSearch(query)` with debounce + `enabled` guard)
- `frontend/src/features/products/keys.ts` (add `search(query)` key)
- `frontend/src/lib/permissions.ts` (add missing `quotations.print` to the `PERMISSIONS` tuple — `quotations.view` is already present, audit and add missing ones)

## Tasks

1. **types.ts** — define:
   ```ts
   export type QuotationStatus = 'Draft' | 'Sent' | 'Confirmed' | 'ConvertedToOrder' | 'Cancelled';
   export type QuotationAction = 'Send' | 'Confirm' | 'Cancel';

   export interface QuotationLine {
     id?: string;
     sortOrder: number;
     productId?: string;
     productCode?: string;
     productName: string;
     specification?: string;
     unitName: string;
     pricingMode: PricingMode;
     length?: number; width?: number; thickness?: number; density?: number; sheetCount?: number;
     quantity: number;
     unitPrice: number;
     lineTotal: number;
     unitCost?: number; lineCost?: number; lineProfit?: number;
     note?: string;
   }

   export interface Quotation { /* full header + lines */ }
   export interface QuotationListItem { id; code; quotationDate; customerName; contactPhone; total; status; createdByName?; createdAt; }
   export interface UpsertQuotationRequest { customerId; quotationDate; customer snapshot fields; delivery fields; taxRate; discount; freight; internalNote?; lines: UpsertQuotationLineRequest[]; }
   export interface UpsertQuotationLineRequest { id?; sortOrder; productId?; productCode?; productName; specification?; unitName; pricingMode; dimensions; quantity; unitPrice; unitCost?; note?; }
   export interface QuotationListParams { page?; pageSize?; status?; customerId?; from?; to?; sortBy?; sortDirection? }
   ```
   Re-export `PagedResult` from `features/customers/types` (as products module already does).

2. **api.ts**:
   ```ts
   import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
   import api from '@/lib/api-client'; // for PDF blob download
   import type { ... } from './types';

   export const quotationsApi = {
     list: (params) => apiGet<PagedResult<QuotationListItem>>('/quotations', params),
     get:  (id)     => apiGet<Quotation>(`/quotations/${id}`),
     create:(data)  => apiPost<Quotation>('/quotations', data),
     update:(id,d)  => apiPut <Quotation>(`/quotations/${id}`, d),
     remove:(id)    => apiDelete(`/quotations/${id}`),
     transition:(id, action) => apiPost<Quotation>(`/quotations/${id}/transition`, { action }),
     downloadPdf: async (id: string): Promise<Blob> => {
       const res = await api.get(`/quotations/${id}/pdf`, { responseType: 'blob' });
       return res.data as Blob;
     },
   };
   ```

3. **keys.ts**:
   ```ts
   export const quotationKeys = {
     all: ['quotations'] as const,
     lists: () => [...quotationKeys.all, 'list'] as const,
     list:  (p) => [...quotationKeys.lists(), p] as const,
     details:() => [...quotationKeys.all, 'detail'] as const,
     detail:(id) => [...quotationKeys.details(), id] as const,
   };
   ```

4. **hooks.ts** — `useQuotations`, `useQuotation`, `useCreateQuotation`, `useUpdateQuotation`, `useDeleteQuotation`, `useTransitionQuotation` (invalidates both list + detail on success). All mirror the shape in `features/customers/hooks.ts`.

5. **schema.ts** — zod for the form:
   ```ts
   const quotationLineSchema = z.object({
     id: z.string().uuid().optional(),
     sortOrder: z.number().int().nonnegative(),
     productId: z.string().uuid().optional(),
     productCode: optionalString(50),
     productName: z.string().min(1, 'Tên hàng là bắt buộc').max(255),
     specification: optionalString(500),
     unitName: z.string().min(1, 'ĐVT là bắt buộc').max(100),
     pricingMode: z.enum(['PerUnit','PerSquareMeter','PerLinearMeter','PerCubicMeter']),
     length: optionalNumber({ min: 0 }),
     width: optionalNumber({ min: 0 }),
     thickness: optionalNumber({ min: 0 }),
     density: optionalNumber({ min: 0 }),
     sheetCount: optionalNumber({ min: 0 }),
     quantity: z.coerce.number().positive('Số lượng phải > 0'),
     unitPrice: z.coerce.number().nonnegative(),
     unitCost: optionalNumber({ min: 0 }),
     note: optionalString(1000),
   });

   export const quotationSchema = z.object({
     customerId: z.string().uuid('Chọn khách hàng'),
     quotationDate: z.string().min(1),                // ISO date string from <input type="date">
     deliveryAddress: optionalString(1000),
     deliveryRecipient: optionalString(255),
     deliveryPhone: optionalString(30),
     deliveryDate: optionalString(20),
     deliveryNote: optionalString(1000),
     taxRate: z.coerce.number().min(0).max(100),
     discount: z.coerce.number().min(0),
     freight: z.coerce.number().min(0),
     internalNote: optionalString(2000),
     lines: z.array(quotationLineSchema).min(1, 'Báo giá phải có ít nhất 1 dòng'),
   });

   export type QuotationFormValues = z.input<typeof quotationSchema>;
   export type QuotationFormParsed = z.output<typeof quotationSchema>;
   ```

6. **Products feature additions**:
   - In `features/products/types.ts` add
     ```ts
     export interface ProductSuggestion { id; code; name; specification?; unitName?; pricingMode: PricingMode; defaultPrice?: number; costPrice?: number; }
     ```
   - In `features/products/api.ts` add `productsApi.search = (q: string, take = 20) => apiGet<ProductSuggestion[]>('/products/search', { q, take });`
   - In `features/products/keys.ts` add `productKeys.search = (q: string) => [...productKeys.all, 'search', q] as const;`
   - In `features/products/hooks.ts` add `useProductSearch(query: string)`:
     ```ts
     export function useProductSearch(query: string) {
       const debounced = useDebouncedValue(query, 200);
       return useQuery({
         queryKey: productKeys.search(debounced),
         queryFn: () => productsApi.search(debounced),
         enabled: debounced.trim().length >= 1,
         staleTime: 30_000,
       });
     }
     ```

7. **Permissions audit** — open `frontend/src/lib/permissions.ts`. The existing list is missing `quotations.delete`, `quotations.print`, `quotations.convert`, `quotations.view_cost`, `orders.delete`, `orders.print`, `orders.view_cost`, `customers.delete`. Add the codes used in this MVP: `quotations.delete`, `quotations.print`. (Do not add the others — only add what this MVP actually references to avoid drift.)

## Verification
```
cd frontend && npm run build && npm test -- --run
```

## Exit Criteria
- `npm run build` succeeds.
- All existing tests still pass.
- New files match the structural conventions used by `features/customers/` and `features/products/`.
