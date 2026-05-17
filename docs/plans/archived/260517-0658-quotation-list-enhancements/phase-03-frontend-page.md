# Phase 03 — Frontend Types + API + Page Integration

**Status:** [x] completed
**Complexity:** M

## Objective
Cập nhật types và api client để khớp với DTO backend mới, sau đó tích hợp 3 cải tiến UI vào `quotation-list-page.tsx`: 3 cột tài chính, dropdown actions, multi-select filter.

## Files
- `frontend/src/features/quotations/types.ts`
- `frontend/src/features/quotations/api.ts`
- `frontend/src/pages/quotations/quotation-list-page.tsx`

## Tasks

### 1. Cập nhật `types.ts`

**a. `QuotationListItem`** (line 69-84): thêm 3 field trước `total`:
```ts
subtotal: number;
discount: number;
freight: number;
```

**b. `QuotationListParams`** (line 127-137): thay
```ts
status?: QuotationStatus;
```
bằng
```ts
statuses?: QuotationStatus[];
```

### 2. Cập nhật `api.ts` — serialize statuses

File: `frontend/src/features/quotations/api.ts`, function `list` (line 13-14). Hiện tại:
```ts
list: (params: QuotationListParams) =>
  apiGet<PagedResult<QuotationListItem>>('/quotations', params),
```

Đổi thành:
```ts
list: (params: QuotationListParams) => {
  // Backend nhận `?status=Draft,Sent` (comma-separated, key tên `status`).
  // Convert FE array thành string trước khi gửi, axios mặc định serialize array thành `?key[]=...`.
  const { statuses, ...rest } = params;
  const serialized: Record<string, unknown> = { ...rest };
  if (statuses && statuses.length > 0) {
    serialized.status = statuses.join(',');
  }
  return apiGet<PagedResult<QuotationListItem>>('/quotations', serialized);
},
```

### 3. Cập nhật `quotation-list-page.tsx`

**a. Imports** (top of file): 
- Bỏ: `Select, SelectContent, SelectItem, SelectTrigger, SelectValue` (line 23-29).
- Giữ icon `Ban` từ `lucide-react` (line 9) — vẫn dùng trong menu item Hủy.
- Thêm `MoreHorizontal` từ `lucide-react`.
- Thêm:
  ```ts
  import { MultiSelect } from '@/components/ui/multi-select';
  import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
  } from '@/components/ui/dropdown-menu';
  ```

**b. Bỏ constant `ALL`** (line 41) — không còn dùng.

**c. State filter statuses** (thay line 67):

Đặt whitelist ở top-of-file (ngay sau `currency` constant) để dùng chung cho cả parse URL và options của MultiSelect:
```ts
const STATUS_OPTIONS: ReadonlyArray<{ value: QuotationStatus; label: string }> = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Sent', label: 'Đã gửi' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'Cancelled', label: 'Đã hủy' },
];
const VALID_STATUSES: ReadonlySet<QuotationStatus> = new Set(STATUS_OPTIONS.map((o) => o.value));
```

Trong component, thay line 67 bằng:
```ts
const [statusParam, setStatusParam] = useSearchParamString('status');
const statuses = useMemo<QuotationStatus[]>(
  () =>
    statusParam
      ? statusParam
          .split(',')
          .filter((s): s is QuotationStatus => VALID_STATUSES.has(s as QuotationStatus))
      : [],
  [statusParam],
);
```
**Lý do filter qua whitelist:** bookmark `?status=Foo,Draft` không làm MultiSelect "count" sai (1 mục ảo) và không nhồi giá trị xấu xuống API (BE đã validator 400 nhưng FE nên defensive). `useMemo` đã import ở line 1 — không cần thêm.

**d. Sửa call `useQuotations`** (line 74-81):
```ts
const { data, isLoading, isError, error } = useQuotations({
  page,
  pageSize: PAGE_SIZE,
  search: debouncedSearch || undefined,
  statuses: statuses.length > 0 ? statuses : undefined,
  from: fromDate || undefined,
  to: toDate || undefined,
});
```

**e. Thêm 3 columns** (vào `columns` array trong `useMemo`, trước column "Tổng tiền" hiện tại):
```ts
{
  header: 'Tổng tiền hàng',
  accessorKey: 'subtotal',
  cell: ({ row }) => (
    <span className="tabular-nums">{currency.format(row.original.subtotal)}</span>
  ),
},
{
  header: 'Chiết khấu',
  accessorKey: 'discount',
  cell: ({ row }) => (
    <span className="tabular-nums">{currency.format(row.original.discount)}</span>
  ),
},
{
  header: 'Vận chuyển',
  accessorKey: 'freight',
  cell: ({ row }) => (
    <span className="tabular-nums">{currency.format(row.original.freight)}</span>
  ),
},
```

**f. Refactor actions cell** (thay block line 125-186):
```tsx
{
  id: 'actions',
  header: '',
  cell: ({ row }) => {
    const q = row.original;
    const canCancel = q.status !== 'Cancelled';
    return (
      <div className="flex justify-end gap-1">
        <Can permission="quotations.update">
          <Button asChild variant="ghost" size="icon" aria-label="Sửa">
            <Link to={`/quotations/${q.id}`}><Pencil className="h-4 w-4" /></Link>
          </Button>
        </Can>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" aria-label="Thao tác khác">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {q.canClone && (
              <Can permission="quotations.create">
                <DropdownMenuItem
                  onClick={() => {
                    clone.mutate(q.id, {
                      onSuccess: (cloned) =>
                        toast({ variant: 'success', title: 'Đã clone báo giá', description: cloned.code }),
                      onError: (err) =>
                        toast({ variant: 'destructive', title: 'Không thể clone', description: getErrorMessage(err) }),
                    });
                  }}
                >
                  <Copy className="mr-2 h-4 w-4" /> Clone
                </DropdownMenuItem>
              </Can>
            )}
            <Can permission="quotations.print">
              <DropdownMenuItem
                onClick={() => {
                  downloadPdf(q.id, q.code).catch((err) =>
                    toast({ variant: 'destructive', title: 'Không tải được PDF', description: getErrorMessage(err) }),
                  );
                }}
              >
                <Printer className="mr-2 h-4 w-4" /> In PDF
              </DropdownMenuItem>
            </Can>
            {canCancel && (
              <Can permission="quotations.update">
                <DropdownMenuItem
                  className="text-destructive focus:text-destructive"
                  onClick={() => setPendingCancel(q)}
                >
                  <Ban className="mr-2 h-4 w-4" /> Hủy
                </DropdownMenuItem>
              </Can>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    );
  },
},
```
**Lưu ý:** vẫn dùng `Pencil`, `Printer`, `Copy`, `Ban` (giữ nguyên import) cùng `MoreHorizontal` mới thêm.

**g. Thay thế filter `Select` (line 243-257)** bằng:
```tsx
<MultiSelect<QuotationStatus>
  options={STATUS_OPTIONS}
  value={statuses}
  onChange={(next) => {
    setStatusParam(next.join(','));
    if (page !== 1) setPage(1);
  }}
  placeholder="Trạng thái"
  triggerClassName="w-44"
  ariaLabel="Trạng thái"
/>
```
Dùng lại `STATUS_OPTIONS` constant đã khai báo ở bước **c** — single source of truth cho label & whitelist.

Lưu ý URL clear: `useSearchParamString` (xem `frontend/src/lib/use-search-param-state.ts:12`) tự `delete(key)` khi value rỗng → gọi `setStatusParam('')` sau khi user "Xóa lọc" sẽ remove `?status` khỏi URL hoàn toàn, không để lại `?status=`.

### 4. Tự kiểm các import & cleanup
- Kiểm tra không còn reference đến `ALL` constant.
- Kiểm tra `Select*` không còn import.
- Đảm bảo `useMemo` được import (line 1, đã có).
- Thêm `React` import nếu chưa có ở top (file đang dùng named imports — đủ).

## Verification

```powershell
cd frontend ; npm run typecheck
```

**Manual smoke test (browser, sau Phase 01)**:
1. `GET /quotations` → bảng có 4 cột tài chính, format `vi-VN` (dấu chấm phẩy nghìn).
2. Dropdown actions: button Sửa hiện riêng, `⋯` chứa các action điều kiện đúng (Clone/PDF/Hủy theo permission + canClone + canCancel).
3. Filter trạng thái:
   - Click trigger → menu mở với 4 checkbox.
   - Click 2 mục → trigger hiển thị "Trạng thái (2)", menu KHÔNG đóng.
   - URL update `?status=Draft,Sent`.
   - Reload page → MultiSelect khôi phục state.
   - Click "Xóa lọc" → URL clear `?status` param.
4. URL legacy `/quotations?status=Draft` → mở thẳng, MultiSelect hiển thị 1 mục đã chọn.
5. Hủy báo giá từ dropdown menu → ConfirmDialog mở, confirm OK.

## Exit Criteria
- `npm run typecheck` pass.
- 5 smoke test ở trên đều OK.
- Không có console error/warning trong devtools khi tương tác với filter và dropdown.
