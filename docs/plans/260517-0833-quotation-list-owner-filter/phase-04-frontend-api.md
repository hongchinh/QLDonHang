# Phase 04 — Frontend: types, API, hook

**Status:** [ ] pending
**Complexity:** S

## Objective
Thêm type `QuotationOwnerOption`, mở rộng `QuotationListParams` với `ownerUserIds`, serialize sang CSV trong `api.ts`, thêm `listOwners` endpoint, và hook `useQuotationOwners` enabled-by-permission.

## Files
- `frontend/src/features/quotations/types.ts` (thêm `QuotationOwnerOption`, mở rộng `QuotationListParams`)
- `frontend/src/features/quotations/api.ts` (serialize `ownerUserIds`, thêm `listOwners`)
- `frontend/src/features/quotations/keys.ts` (thêm key cho owners)
- `frontend/src/features/quotations/hooks.ts` (thêm `useQuotationOwners`)

## Tasks

1. **Sửa `types.ts`** — thêm sau `QuotationListAggregates`:
   ```ts
   export interface QuotationOwnerOption {
     id: string;
     fullName: string;
     isDeleted: boolean;
     quotationCount: number;
   }
   ```
   Mở rộng `QuotationListParams`:
   ```ts
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
     ownerUserIds?: string[];  // NEW
   }
   ```

2. **Sửa `api.ts`** — update `list` để serialize `ownerUserIds` thành CSV và thêm `listOwners`:
   ```ts
   export const quotationsApi = {
     list: (params: QuotationListParams) => {
       const { statuses, ownerUserIds, ...rest } = params;
       const serialized: Record<string, unknown> = { ...rest };
       if (statuses && statuses.length > 0) {
         serialized.status = statuses.join(',');
       }
       if (ownerUserIds && ownerUserIds.length > 0) {
         serialized.ownerUserIds = ownerUserIds.join(',');
       }
       return apiGet<QuotationListResult>('/quotations', serialized);
     },
     listOwners: (includeDeleted = true) =>
       apiGet<QuotationOwnerOption[]>('/quotations/owners', { includeDeleted }),
     // ... rest unchanged
   };
   ```
   Nhớ import `QuotationOwnerOption` trong type import header.

3. **Sửa `keys.ts`** — thêm key namespace cho owners:
   ```ts
   export const quotationKeys = {
     all: ['quotations'] as const,
     lists: () => [...quotationKeys.all, 'list'] as const,
     list: (params: QuotationListParams) => [...quotationKeys.lists(), params] as const,
     details: () => [...quotationKeys.all, 'detail'] as const,
     detail: (id: string) => [...quotationKeys.details(), id] as const,
     owners: (includeDeleted: boolean) =>            // NEW
       [...quotationKeys.all, 'owners', { includeDeleted }] as const,
   };
   ```

4. **Sửa `hooks.ts`** — thêm `useQuotationOwners` ở cuối file:
   ```ts
   export function useQuotationOwners(opts: { includeDeleted?: boolean; enabled: boolean }) {
     const includeDeleted = opts.includeDeleted ?? true;
     return useQuery({
       queryKey: quotationKeys.owners(includeDeleted),
       queryFn: () => quotationsApi.listOwners(includeDeleted),
       enabled: opts.enabled,
       staleTime: 5 * 60_000,
     });
   }
   ```
   (Không import gì mới — đã có `useQuery`, `quotationsApi`, `quotationKeys`.)

## Verification
```powershell
cd frontend
npm run typecheck
```
- Typecheck xanh.
- Inspect: `useQuotationOwners` xuất hiện trong `hooks.ts`; `QuotationOwnerOption` xuất hiện trong `types.ts`.

## Exit Criteria
- [ ] `QuotationOwnerOption` interface tồn tại.
- [ ] `QuotationListParams.ownerUserIds?: string[]` tồn tại.
- [ ] `quotationsApi.list` serialize `ownerUserIds` thành CSV với key `ownerUserIds`.
- [ ] `quotationsApi.listOwners` tồn tại, default `includeDeleted=true`.
- [ ] `quotationKeys.owners(includeDeleted)` tồn tại.
- [ ] `useQuotationOwners` hook tồn tại, `staleTime: 5 phút`, `enabled` từ opts.
- [ ] `npm run typecheck` xanh.
