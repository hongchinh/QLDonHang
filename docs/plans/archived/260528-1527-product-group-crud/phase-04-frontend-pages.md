# Phase 04 — Frontend: Pages & App Wiring

**Status:** [ ] pending
**Complexity:** M

## Objective

Build the list page and create/edit dialog, then wire the route and sidebar nav item.
After this phase, users can navigate to `/product-groups` and perform full CRUD.

## Files

- `frontend/src/pages/product-groups/product-group-form-dialog.tsx` *(new)*
- `frontend/src/pages/product-groups/product-group-list-page.tsx` *(new)*
- `frontend/src/App.tsx` *(edit — add route)*
- `frontend/src/components/layout/app-layout.tsx` *(edit — add sidebar nav item)*

## Tasks

### Task 1 — Create product-group-form-dialog.tsx

Create `frontend/src/pages/product-groups/product-group-form-dialog.tsx`:

```tsx
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  useCreateProductGroup,
  useUpdateProductGroup,
} from '@/features/product-groups/hooks';
import {
  productGroupSchema,
  type ProductGroupFormParsed,
  type ProductGroupFormValues,
} from '@/features/product-groups/schema';
import type {
  CreateProductGroupRequest,
  ProductGroupListItem,
  UpdateProductGroupRequest,
} from '@/features/product-groups/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { getErrorMessage } from '@/lib/api-client';
import { toast } from '@/lib/use-toast';
import { cn } from '@/lib/utils';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Pass an existing item to enter edit mode; omit for create mode. */
  initial?: ProductGroupListItem;
}

export function ProductGroupFormDialog({ open, onOpenChange, initial }: Props) {
  const isEdit = !!initial;
  const create = useCreateProductGroup();
  const update = useUpdateProductGroup();

  const form = useForm<ProductGroupFormValues, unknown, ProductGroupFormParsed>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(productGroupSchema) as any,
    defaultValues: toDefaults(initial),
  });

  // Reset form when switching between create/edit mode or when the dialog opens.
  useEffect(() => {
    if (open) form.reset(toDefaults(initial));
  }, [open, initial]); // eslint-disable-line react-hooks/exhaustive-deps

  const isPending = create.isPending || update.isPending;

  const onSubmit = async (parsed: ProductGroupFormParsed) => {
    try {
      if (isEdit && initial) {
        await update.mutateAsync({ id: initial.id, data: toUpdatePayload(parsed) });
        toast({ variant: 'success', title: 'Đã cập nhật nhóm hàng hóa' });
      } else {
        await create.mutateAsync(toCreatePayload(parsed));
        toast({ variant: 'success', title: 'Đã tạo nhóm hàng hóa' });
      }
      onOpenChange(false);
    } catch (err) {
      toast({ variant: 'destructive', title: 'Không thể lưu', description: getErrorMessage(err) });
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Chỉnh sửa nhóm hàng hóa' : 'Thêm nhóm hàng hóa'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 pt-2">
          {!isEdit && (
            <FormField
              label="Mã nhóm"
              hint="Để trống để tự sinh"
              name="code"
              form={form}
            />
          )}

          <FormField label="Tên nhóm *" name="name" form={form} />

          <div className="space-y-2">
            <Label htmlFor="description">Mô tả</Label>
            <Textarea
              id="description"
              rows={2}
              {...form.register('description')}
            />
            {form.formState.errors.description && (
              <p className="text-sm text-destructive">
                {String(form.formState.errors.description.message)}
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Thứ tự" name="sortOrder" type="number" form={form} />

            <div className="space-y-2">
              <Label htmlFor="isActive">Trạng thái</Label>
              <div className="flex items-center gap-2 pt-1">
                <input
                  id="isActive"
                  type="checkbox"
                  className="h-4 w-4"
                  {...form.register('isActive')}
                />
                <span className="text-sm">Đang hoạt động</span>
              </div>
            </div>
          </div>

          {(create.isError || update.isError) && (
            <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              {getErrorMessage(create.error ?? update.error)}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isPending}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo mới'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function toDefaults(item?: ProductGroupListItem): ProductGroupFormValues {
  return {
    code: item?.code ?? '',
    name: item?.name ?? '',
    description: item?.description ?? '',
    sortOrder: item?.sortOrder ?? 0,
    isActive: item?.isActive ?? true,
  };
}

function toCreatePayload(p: ProductGroupFormParsed): CreateProductGroupRequest {
  return {
    code: p.code || undefined,
    name: p.name,
    description: p.description || undefined,
    sortOrder: p.sortOrder,
    isActive: p.isActive,
  };
}

function toUpdatePayload(p: ProductGroupFormParsed): UpdateProductGroupRequest {
  return {
    name: p.name,
    description: p.description || undefined,
    sortOrder: p.sortOrder,
    isActive: p.isActive,
  };
}

interface FormFieldProps {
  label: string;
  name: keyof ProductGroupFormValues;
  type?: string;
  hint?: string;
  className?: string;
  form: ReturnType<typeof useForm<ProductGroupFormValues, unknown, ProductGroupFormParsed>>;
}

function FormField({ label, name, type = 'text', hint, className, form }: FormFieldProps) {
  const error = form.formState.errors[name];
  return (
    <div className={cn('space-y-2', className)}>
      <Label htmlFor={String(name)}>{label}</Label>
      <Input id={String(name)} type={type} {...form.register(name)} />
      {hint && !error && <p className="text-xs text-muted-foreground">{hint}</p>}
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}
```

### Task 2 — Create product-group-list-page.tsx

Create `frontend/src/pages/product-groups/product-group-list-page.tsx`:

```tsx
import { useMemo, useState } from 'react';
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from '@tanstack/react-table';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';
import {
  useDeleteProductGroup,
  useProductGroups,
} from '@/features/product-groups/hooks';
import type { ProductGroupListItem } from '@/features/product-groups/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Card, CardContent } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Can } from '@/components/auth/can';
import { useDebouncedValue } from '@/lib/use-debounced-value';
import { useSearchParamNumber, useSearchParamString } from '@/lib/use-search-param-state';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { ProductGroupFormDialog } from './product-group-form-dialog';

const PAGE_SIZE = 20;

export function ProductGroupListPage() {
  const [search, setSearch] = useSearchParamString('q');
  const [page, setPage] = useSearchParamNumber('page', 1);
  const debouncedSearch = useDebouncedValue(search, 300);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<ProductGroupListItem | undefined>();
  const [pendingDelete, setPendingDelete] = useState<ProductGroupListItem | null>(null);

  const { data, isLoading, isError, error } = useProductGroups({
    page,
    pageSize: PAGE_SIZE,
    search: debouncedSearch || undefined,
  });

  const remove = useDeleteProductGroup();

  const openCreate = () => {
    setEditTarget(undefined);
    setDialogOpen(true);
  };

  const openEdit = (item: ProductGroupListItem) => {
    setEditTarget(item);
    setDialogOpen(true);
  };

  const columns = useMemo<ColumnDef<ProductGroupListItem>[]>(
    () => [
      { header: 'Mã', accessorKey: 'code' },
      { header: 'Tên nhóm', accessorKey: 'name' },
      { header: 'Mô tả', accessorKey: 'description' },
      { header: 'Thứ tự', accessorKey: 'sortOrder' },
      {
        header: 'Số hàng hóa',
        accessorKey: 'productCount',
        cell: ({ row }) => (
          <span className="tabular-nums">{row.original.productCount}</span>
        ),
      },
      {
        header: 'Trạng thái',
        accessorKey: 'isActive',
        cell: ({ row }) =>
          row.original.isActive ? (
            <Badge variant="success">Hoạt động</Badge>
          ) : (
            <Badge variant="secondary">Không hoạt động</Badge>
          ),
      },
      {
        id: 'actions',
        header: '',
        cell: ({ row }) => (
          <div className="flex justify-end gap-2">
            <Can permission="products.update">
              <Button
                variant="ghost"
                size="icon"
                aria-label="Sửa nhóm hàng hóa"
                onClick={() => openEdit(row.original)}
              >
                <Pencil className="h-4 w-4 text-blue-600" />
              </Button>
            </Can>
            <Can permission="products.delete">
              <Button
                variant="ghost"
                size="icon"
                aria-label="Xóa nhóm hàng hóa"
                onClick={() => setPendingDelete(row.original)}
              >
                <Trash2 className="h-4 w-4 text-red-600" />
              </Button>
            </Can>
          </div>
        ),
      },
    ],
    [], // eslint-disable-line react-hooks/exhaustive-deps
  );

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  const onConfirmDelete = () => {
    if (!pendingDelete) return;
    const target = pendingDelete;
    remove.mutate(target.id, {
      onSuccess: () => {
        toast({ variant: 'success', title: 'Đã xóa nhóm hàng hóa', description: target.name });
        setPendingDelete(null);
      },
      onError: (err) => {
        toast({ variant: 'destructive', title: 'Không thể xóa', description: getErrorMessage(err) });
      },
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Nhóm hàng hóa</h1>
          <p className="text-sm text-muted-foreground">Quản lý danh mục nhóm hàng hóa</p>
        </div>
        <Can permission="products.create">
          <Button onClick={openCreate}>
            <Plus className="mr-2 h-4 w-4 text-cyan-600" /> Thêm nhóm
          </Button>
        </Can>
      </div>

      <Card>
        <CardContent className="p-4">
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <div className="relative max-w-sm flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
              <Input
                placeholder="Tìm theo tên / mã..."
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  if (page !== 1) setPage(1);
                }}
                className="pl-9"
              />
            </div>
          </div>

          {isError && (
            <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              {getErrorMessage(error)}
            </div>
          )}

          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((hg) => (
                <TableRow key={hg.id}>
                  {hg.headers.map((h) => (
                    <TableHead key={h.id}>
                      {flexRender(h.column.columnDef.header, h.getContext())}
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                    Đang tải...
                  </TableCell>
                </TableRow>
              ) : table.getRowModel().rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                    Chưa có nhóm hàng hóa nào.
                  </TableCell>
                </TableRow>
              ) : (
                table.getRowModel().rows.map((row) => (
                  <TableRow key={row.id}>
                    {row.getVisibleCells().map((c) => (
                      <TableCell key={c.id}>
                        {flexRender(c.column.columnDef.cell, c.getContext())}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>

          {data && data.totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between text-sm">
              <div className="text-muted-foreground">
                Trang {data.page} / {data.totalPages} • Tổng {data.totalItems}
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={!data.hasPreviousPage} onClick={() => setPage(page - 1)}>
                  Trước
                </Button>
                <Button variant="outline" size="sm" disabled={!data.hasNextPage} onClick={() => setPage(page + 1)}>
                  Sau
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <ProductGroupFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        initial={editTarget}
      />

      <ConfirmDialog
        open={!!pendingDelete}
        onOpenChange={(open) => !open && setPendingDelete(null)}
        title="Xóa nhóm hàng hóa?"
        description={
          pendingDelete ? (
            <>
              Bạn chắc chắn muốn xóa nhóm <strong>{pendingDelete.name}</strong>?
              {pendingDelete.productCount > 0 && (
                <> Có <strong>{pendingDelete.productCount}</strong> hàng hóa liên kết sẽ bị gỡ khỏi nhóm này.</>
              )}
            </>
          ) : null
        }
        destructive
        confirmLabel="Xóa"
        loading={remove.isPending}
        onConfirm={onConfirmDelete}
      />
    </div>
  );
}
```

### Task 3 — Add route in App.tsx

Edit `frontend/src/App.tsx`.

1. Add import at the top with the other page imports:
```typescript
import { ProductGroupListPage } from '@/pages/product-groups/product-group-list-page';
```

2. Add the route inside the `<Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>` block,
   after the `products` route block:
```tsx
<Route path="product-groups">
  <Route
    index
    element={
      <ProtectedRoute permission="products.view">
        <ProductGroupListPage />
      </ProtectedRoute>
    }
  />
</Route>
```

### Task 4 — Add sidebar nav item in app-layout.tsx

Edit `frontend/src/components/layout/app-layout.tsx`.

In the `navGroups` array, find the `'Chức năng'` group and add `'Nhóm hàng hóa'` as the
item immediately after `'Hàng hóa'`:

```typescript
{ to: '/product-groups', label: 'Nhóm hàng hóa', icon: Tag, permission: 'products.view' },
```

Also add `Tag` to the lucide-react import at the top of `app-layout.tsx`:
```typescript
import {
  LayoutDashboard,
  Users,
  Package,
  Tag,       // ← add this
  FileText,
  BarChart3,
  UserCog,
  Users2,
  ShieldCheck,
  Settings,
} from 'lucide-react';
```

### Task 5 — Final type-check and visual review

```bash
cd frontend && npm run typecheck
```

Expected: 0 errors.

Then start the dev server and manually verify:
1. Navigate to `/product-groups` — list page loads with data.
2. Click "Thêm nhóm" — dialog opens with empty form.
3. Fill in Name only, submit — auto-generated code, success toast, list refreshes.
4. Click edit (✏️) on a row — dialog opens pre-filled.
5. Update name, save — toast, list refreshes with new name.
6. Click delete (🗑️) on a row — confirm dialog shows productCount warning.
7. Confirm delete — row disappears from list.
8. Open ProductForm (`/products/new`) — group dropdown shows newly created group immediately.

### Task 6 — Commit

```bash
git add frontend/src/pages/product-groups/ \
        frontend/src/App.tsx \
        frontend/src/components/layout/app-layout.tsx
git commit -m "feat: add product-group list page, form dialog, route, and sidebar nav"
```

## Verification

```bash
cd frontend && npm run typecheck
```

Expected: 0 errors.

Manual browser walkthrough of 8 steps listed in Task 5.

## Exit Criteria

- `npm run typecheck` exits with 0 errors.
- All 8 manual verification steps pass without console errors.
- Sidebar shows "Nhóm hàng hóa" below "Hàng hóa" for users with `products.view` permission.
- ProductForm group dropdown reflects creates/updates from the product-groups page without
  requiring a page reload (cache invalidation working).
