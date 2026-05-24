# Phase 05 — Frontend: UI integration + URL state + tests

**Status:** [ ] pending
**Complexity:** M

## Objective
Thêm parse URL param `ownerUserIds=...` thành `string[]`, gọi `useQuotationOwners` khi `hasViewAll`, render `<MultiSelect>` "Chủ sở hữu" cạnh filter status. Truyền `ownerUserIds` vào `useQuotations`. Test pure parse util.

**Naming**: URL key = `ownerUserIds` (đồng bộ với BE binding của `QuotationListRequest.OwnerUserIds`). FE serializer trong `api.ts` cũng dùng key này khi gọi `/quotations` — end-to-end identical.

## Files
- `frontend/src/pages/quotations/utils/owner-ids.ts` (mới — pure helper)
- `frontend/src/pages/quotations/utils/owner-ids.test.ts` (mới — vitest)
- `frontend/src/pages/quotations/quotation-list-page.tsx` (sửa)

## Tasks

1. **Tạo `utils/owner-ids.ts`** — pure parse helper:
   ```ts
   const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

   export function parseOwnerIds(raw: string): string[] {
     if (!raw) return [];
     return raw
       .split(',')
       .map((s) => s.trim())
       .filter((s) => GUID_RE.test(s));
   }
   ```

2. **Tạo `utils/owner-ids.test.ts`**:
   ```ts
   import { describe, it, expect } from 'vitest';
   import { parseOwnerIds } from './owner-ids';

   describe('parseOwnerIds', () => {
     it('returns empty for empty input', () => {
       expect(parseOwnerIds('')).toEqual([]);
     });

     it('parses single guid', () => {
       const g = '11111111-1111-1111-1111-111111111111';
       expect(parseOwnerIds(g)).toEqual([g]);
     });

     it('parses csv guids', () => {
       const a = '11111111-1111-1111-1111-111111111111';
       const b = '22222222-2222-2222-2222-222222222222';
       expect(parseOwnerIds(`${a},${b}`)).toEqual([a, b]);
     });

     it('filters out non-guid tokens', () => {
       const a = '11111111-1111-1111-1111-111111111111';
       expect(parseOwnerIds(`${a},not-a-guid,abc`)).toEqual([a]);
     });

     it('trims whitespace around tokens', () => {
       const a = '11111111-1111-1111-1111-111111111111';
       const b = '22222222-2222-2222-2222-222222222222';
       expect(parseOwnerIds(` ${a} , ${b} `)).toEqual([a, b]);
     });
   });
   ```

3. **Sửa `quotation-list-page.tsx`**:

   **Imports** — thêm:
   ```tsx
   import { useQuotations, useTransitionQuotation, useCloneQuotation, useQuotationOwners } from '@/features/quotations/hooks';
   import { parseOwnerIds } from './utils/owner-ids';
   ```

   **Trong component**, sau khối parse `statuses` (~dòng 81-89), thêm:
   ```tsx
   const [ownerIdsParam, setOwnerIdsParam] = useSearchParamString('ownerUserIds');

   const ownerIds = useMemo<string[]>(
     () => (hasViewAll ? parseOwnerIds(ownerIdsParam) : []),
     [hasViewAll, ownerIdsParam],
   );
   ```
   Lưu ý: `hasViewAll` được khai báo ở dòng 103 hiện tại — cần **chuyển khai báo `hasViewAll` lên trước** block này (ngay sau `useSearchParamString`/`useDebouncedValue`, trước `pendingCancel` state).

   **Trong `useQuotations` call** (~dòng 93-100), thêm `ownerUserIds`:
   ```tsx
   const { data, isLoading, isError, error } = useQuotations({
     page,
     pageSize: PAGE_SIZE,
     search: debouncedSearch || undefined,
     statuses: statuses.length > 0 ? statuses : undefined,
     ownerUserIds: ownerIds.length > 0 ? ownerIds : undefined,
     from: fromDate || undefined,
     to: toDate || undefined,
   });
   ```

   **Thêm hook owners** sau `const clone = useCloneQuotation();`:
   ```tsx
   const ownersQuery = useQuotationOwners({ enabled: hasViewAll });
   const ownerOptions = useMemo(() => {
     const list = ownersQuery.data ?? [];
     return list.map((o) => ({
       value: o.id,
       label: o.isDeleted ? `${o.fullName} (đã nghỉ)` : o.fullName,
     }));
   }, [ownersQuery.data]);
   ```

   **Render `<MultiSelect>`** trong toolbar filter — đặt ngay SAU `<MultiSelect>` status (~dòng 309), TRƯỚC 2 input date:
   ```tsx
   {hasViewAll && (
     <MultiSelect<string>
       options={ownerOptions}
       value={ownerIds}
       onChange={(next) => {
         setOwnerIdsParam(next.join(','));
         if (page !== 1) setPage(1);
       }}
       placeholder="Chủ sở hữu"
       triggerClassName="w-56"
       ariaLabel="Chủ sở hữu"
     />
   )}
   ```

## Verification
```powershell
cd frontend
npm run typecheck
npm run lint
npm test -- --run src/pages/quotations/utils/owner-ids.test.ts
```
- Typecheck + lint xanh.
- 5 test PASS.

Manual smoke (browser, WebApi đã restart sau Phase 02):
1. Login `admin / Admin@123` → /quotations: thấy dropdown "Chủ sở hữu" cạnh "Trạng thái". Mở dropdown → có ít nhất admin trong list.
2. Tạo 1 sale user qua admin UI, login sale, tạo 1 báo giá.
3. Login admin → reload /quotations → dropdown owners có thêm sale. Chọn sale → URL thành `?ownerUserIds=<guid>`, list chỉ còn báo giá của sale, footer totals khớp.
4. Bỏ chọn → URL bỏ `ownerUserIds`, list về tất cả.
5. Login sale → /quotations: KHÔNG thấy filter "Chủ sở hữu". Thử URL `?ownerUserIds=<adminId>` → list vẫn chỉ thấy báo giá của sale (FE không gửi param vì `hasViewAll=false`).
6. Admin xóa sale → reload → dropdown owners vẫn có sale với badge "(đã nghỉ)" ở cuối, filter ra báo giá orphan.

## Exit Criteria
- [ ] File `utils/owner-ids.ts` + test file tồn tại; 5 test PASS.
- [ ] `quotation-list-page.tsx` import `useQuotationOwners` và `parseOwnerIds`.
- [ ] Khai báo `hasViewAll` được dời lên trước block parse `ownerIds`.
- [ ] `ownerIds` chỉ non-empty khi `hasViewAll === true`.
- [ ] `useQuotations` nhận `ownerUserIds` param.
- [ ] `<MultiSelect>` "Chủ sở hữu" chỉ render khi `hasViewAll`, đặt sau Status filter và trước date inputs.
- [ ] Typecheck + lint xanh.
- [ ] Manual smoke: tất cả 6 case mô tả ở Verification PASS.
