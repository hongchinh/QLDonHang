# Phase 01 — ProductFormDialog component

**Status:** [ ] pending
**Complexity:** M

## Objective

Create the standalone `ProductFormDialog` component that handles both create and edit modes for a product. It is self-contained and reusable from both the catalog dialog and a future `/products` page.

## Files

- `frontend/src/features/products/components/product-form-dialog.tsx` (new)
- `frontend/src/features/products/components/product-form-dialog.test.tsx` (new)

## Reference files (read-only)

- `frontend/src/pages/product-groups/product-group-form-dialog.tsx` — exact structural pattern to follow
- `frontend/src/features/products/schema.ts` — `productSchema`, `ProductFormValues`, `ProductFormParsed`
- `frontend/src/features/products/hooks.ts` — `useCreateProduct`, `useUpdateProduct`, `useProductGroups`, `useUnits`
- `frontend/src/features/products/types.ts` — `Product`, `ProductListItem`
- `frontend/src/stores/auth-store.ts` — `useAuthStore(s => s.hasPermission(...))`
- `frontend/src/components/ui/select.tsx` — `Select`, `SelectTrigger`, `SelectValue`, `SelectContent`, `SelectItem`

## Tasks

### Task 1.1 — Write failing tests for ProductFormDialog

1. Create `frontend/src/features/products/components/product-form-dialog.test.tsx` with the following failing tests (no implementation yet):

   ```tsx
   import { render, screen } from '@testing-library/react';
   import { beforeEach, describe, expect, it, vi } from 'vitest';
   import { ProductFormDialog } from './product-form-dialog';

   vi.mock('@/features/products/hooks', () => ({
     useCreateProduct: () => ({ mutateAsync: vi.fn(), isPending: false, isError: false, error: null }),
     useUpdateProduct: () => ({ mutateAsync: vi.fn(), isPending: false, isError: false, error: null }),
     useProductGroups: () => ({ data: [{ id: 'g1', code: 'G1', name: 'Nhóm A' }] }),
     useUnits: () => ({ data: [{ id: 'u1', code: 'CAI', name: 'Cái' }] }),
   }));

   const mockHasPermission = vi.fn().mockReturnValue(true);

   vi.mock('@/stores/auth-store', () => ({
     useAuthStore: (selector: (s: { hasPermission: (p: string) => boolean }) => unknown) =>
       selector({ hasPermission: (p: string) => mockHasPermission(p) }),
   }));

   vi.mock('@/lib/use-toast', () => ({ toast: vi.fn() }));

   describe('ProductFormDialog', () => {
     beforeEach(() => { mockHasPermission.mockReturnValue(true); });

     it('renders create title when no initial prop', () => {
       render(<ProductFormDialog open onOpenChange={vi.fn()} />);
       expect(screen.getByText('Thêm hàng hóa')).toBeInTheDocument();
     });

     it('renders edit title when initial prop provided', () => {
       const item = {
         id: 'p1', code: 'HH-001', name: 'Sản phẩm A',
         status: 'Active' as const, pricingMode: 'PerUnit' as const,
       };
       render(<ProductFormDialog open onOpenChange={vi.fn()} initial={item} />);
       expect(screen.getByText('Chỉnh sửa hàng hóa')).toBeInTheDocument();
     });

     it('shows Mã hàng field only in create mode', () => {
       render(<ProductFormDialog open onOpenChange={vi.fn()} />);
       expect(screen.getByLabelText(/Mã hàng/)).toBeInTheDocument();
     });

     it('hides Mã hàng field in edit mode', () => {
       const item = {
         id: 'p1', code: 'HH-001', name: 'Sản phẩm A',
         status: 'Active' as const, pricingMode: 'PerUnit' as const,
       };
       render(<ProductFormDialog open onOpenChange={vi.fn()} initial={item} />);
       expect(screen.queryByLabelText(/Mã hàng/)).toBeNull();
     });

     it('does not show Trạng thái field in create mode', () => {
       render(<ProductFormDialog open onOpenChange={vi.fn()} />);
       expect(screen.queryByLabelText(/Trạng thái/)).toBeNull();
     });

     it('hides Giá nhập when user lacks quotations.view_cost', () => {
       mockHasPermission.mockImplementation((p: string) => p !== 'quotations.view_cost');
       render(<ProductFormDialog open onOpenChange={vi.fn()} />);
       expect(screen.queryByLabelText(/Giá nhập/)).toBeNull();
     });
   });
   ```

2. Run: `cd frontend && npx vitest run src/features/products/components/product-form-dialog.test.tsx`
   Expected: FAIL — `Cannot find module './product-form-dialog'`

### Task 1.2 — Implement ProductFormDialog

3. Create `frontend/src/features/products/components/product-form-dialog.tsx` with the following structure:

   **Imports:**
   ```tsx
   import { useEffect } from 'react';
   import { useForm, Controller } from 'react-hook-form';
   import { zodResolver } from '@hookform/resolvers/zod';
   import {
     useCreateProduct, useUpdateProduct, useProductGroups, useUnits,
   } from '@/features/products/hooks';
   import { productSchema, type ProductFormValues, type ProductFormParsed } from '@/features/products/schema';
   import type { Product, ProductListItem, CreateProductRequest, UpdateProductRequest } from '@/features/products/types';
   import { useAuthStore } from '@/stores/auth-store';
   import { Button } from '@/components/ui/button';
   import { Input } from '@/components/ui/input';
   import { Label } from '@/components/ui/label';
   import { Textarea } from '@/components/ui/textarea';
   import {
     Select, SelectTrigger, SelectValue, SelectContent, SelectItem,
   } from '@/components/ui/select';
   import {
     Dialog, DialogContent, DialogHeader, DialogTitle,
   } from '@/components/ui/dialog';
   import { getErrorMessage } from '@/lib/api-client';
   import { toast } from '@/lib/use-toast';
   import { cn } from '@/lib/utils';
   ```

   **Props interface:**
   ```tsx
   interface Props {
     open: boolean;
     onOpenChange: (open: boolean) => void;
     initial?: ProductListItem;
     onCreated?: (product: Product) => void;
   }
   ```

   **Component structure:**
   ```tsx
   export function ProductFormDialog({ open, onOpenChange, initial, onCreated }: Props) {
     const isEdit = !!initial;
     const create = useCreateProduct();
     const update = useUpdateProduct();
     const groups = useProductGroups();
     const units = useUnits();
     const canViewCost = useAuthStore((s) => s.hasPermission('quotations.view_cost'));

     const form = useForm<ProductFormValues, unknown, ProductFormParsed>({
       resolver: zodResolver(productSchema) as any,
       defaultValues: toDefaults(initial),
     });

     useEffect(() => {
       if (open) form.reset(toDefaults(initial));
     }, [open, initial]); // eslint-disable-line react-hooks/exhaustive-deps

     const isPending = create.isPending || update.isPending;

     const onSubmit = async (parsed: ProductFormParsed) => {
       try {
         if (isEdit && initial) {
           await update.mutateAsync({ id: initial.id, data: toUpdatePayload(parsed) });
           toast({ variant: 'success', title: 'Đã cập nhật hàng hóa' });
           onOpenChange(false);
         } else {
           const product = await create.mutateAsync(toCreatePayload(parsed));
           toast({ variant: 'success', title: 'Đã tạo hàng hóa' });
           onCreated?.(product);
           onOpenChange(false);
         }
       } catch (err) {
         toast({ variant: 'destructive', title: 'Không thể lưu', description: getErrorMessage(err) });
       }
     };

     return (
       <Dialog open={open} onOpenChange={onOpenChange}>
         <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
           <DialogHeader>
             <DialogTitle>{isEdit ? 'Chỉnh sửa hàng hóa' : 'Thêm hàng hóa'}</DialogTitle>
           </DialogHeader>

           <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5 pt-2">
             {/* ── Section 1: Thông tin cơ bản ── */}
             <fieldset className="space-y-3">
               <legend className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">
                 Thông tin cơ bản
               </legend>

               {!isEdit && (
                 <FormField label="Mã hàng" name="code" hint="Để trống để tự sinh" form={form} />
               )}

               <FormField label="Tên hàng *" name="name" form={form} />

               {/* Nhóm hàng dropdown */}
               <div className="space-y-2">
                 <Label htmlFor="productGroupId">Nhóm hàng *</Label>
                 <Controller
                   control={form.control}
                   name="productGroupId"
                   render={({ field }) => (
                     <Select value={field.value ?? ''} onValueChange={field.onChange}>
                       <SelectTrigger id="productGroupId">
                         <SelectValue placeholder="Chọn nhóm hàng..." />
                       </SelectTrigger>
                       <SelectContent>
                         {(groups.data ?? []).map((g) => (
                           <SelectItem key={g.id} value={g.id}>
                             {g.name}
                           </SelectItem>
                         ))}
                       </SelectContent>
                     </Select>
                   )}
                 />
                 {form.formState.errors.productGroupId && (
                   <p className="text-sm text-destructive">
                     {String(form.formState.errors.productGroupId.message)}
                   </p>
                 )}
               </div>

               {/* ĐVT dropdown */}
               <div className="space-y-2">
                 <Label htmlFor="unitId">ĐVT *</Label>
                 <Controller
                   control={form.control}
                   name="unitId"
                   render={({ field }) => (
                     <Select value={field.value ?? ''} onValueChange={field.onChange}>
                       <SelectTrigger id="unitId">
                         <SelectValue placeholder="Chọn đơn vị..." />
                       </SelectTrigger>
                       <SelectContent>
                         {(units.data ?? []).map((u) => (
                           <SelectItem key={u.id} value={u.id}>
                             {u.name}
                           </SelectItem>
                         ))}
                       </SelectContent>
                     </Select>
                   )}
                 />
                 {form.formState.errors.unitId && (
                   <p className="text-sm text-destructive">
                     {String(form.formState.errors.unitId.message)}
                   </p>
                 )}
               </div>

               <FormField label="Quy cách" name="specification" form={form} />

               {isEdit && (
                 <div className="space-y-2">
                   <Label htmlFor="status">Trạng thái</Label>
                   <div className="flex items-center gap-2 pt-1">
                     <input
                       id="status"
                       type="checkbox"
                       className="h-4 w-4"
                       checked={form.watch('status') === 'Active'}
                       onChange={(e) =>
                         form.setValue('status', e.target.checked ? 'Active' : 'Inactive')
                       }
                     />
                     <span className="text-sm">Đang hoạt động</span>
                   </div>
                 </div>
               )}
             </fieldset>

             {/* ── Section 2: Kích thước & Giá ── */}
             <fieldset className="space-y-3">
               <legend className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-2">
                 Kích thước & Giá
               </legend>

               {/* Loại tính giá dropdown */}
               <div className="space-y-2">
                 <Label htmlFor="pricingMode">Loại tính giá *</Label>
                 <Controller
                   control={form.control}
                   name="pricingMode"
                   render={({ field }) => (
                     <Select value={field.value ?? ''} onValueChange={field.onChange}>
                       <SelectTrigger id="pricingMode">
                         <SelectValue placeholder="Chọn loại tính giá..." />
                       </SelectTrigger>
                       <SelectContent>
                         <SelectItem value="PerUnit">Theo đơn vị</SelectItem>
                         <SelectItem value="PerSquareMeter">Theo diện tích (m²)</SelectItem>
                         <SelectItem value="PerLinearMeter">Theo dài (m)</SelectItem>
                         <SelectItem value="PerCubicMeter">Theo thể tích (m³)</SelectItem>
                       </SelectContent>
                     </Select>
                   )}
                 />
                 {form.formState.errors.pricingMode && (
                   <p className="text-sm text-destructive">
                     {String(form.formState.errors.pricingMode.message)}
                   </p>
                 )}
               </div>

               {/* Dimensions row */}
               <div className="grid grid-cols-3 gap-3">
                 <FormField label="Dài (mm)" name="length" type="number" form={form} />
                 <FormField label="Rộng (mm)" name="width" type="number" form={form} />
                 <FormField label="Dày (mm)" name="thickness" type="number" form={form} />
               </div>

               <FormField label="Mật độ (kg/m³)" name="density" type="number" form={form} />

               <FormField label="Giá bán" name="defaultPrice" type="number" form={form} />

               {canViewCost && (
                 <FormField label="Giá nhập" name="costPrice" type="number" form={form} />
               )}

               <FormField label="Thuế (%)" name="defaultTaxRate" type="number" form={form} />

               <div className="space-y-2">
                 <Label htmlFor="note">Ghi chú</Label>
                 <Textarea id="note" rows={2} {...form.register('note')} />
                 {form.formState.errors.note && (
                   <p className="text-sm text-destructive">
                     {String(form.formState.errors.note.message)}
                   </p>
                 )}
               </div>
             </fieldset>

             {(create.isError || update.isError) && (
               <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
                 {getErrorMessage(create.error ?? update.error)}
               </div>
             )}

             <div className="flex justify-end gap-2 pt-2">
               <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
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
   ```

   **Helper functions (after component):**
   ```tsx
   function toDefaults(item?: ProductListItem): ProductFormValues {
     return {
       code: '',
       name: item?.name ?? '',
       productGroupId: '',   // ProductListItem has no groupId; left empty — user must re-select for edit
       unitId: '',           // Same — user must confirm
       specification: item?.specification ?? '',
       pricingMode: item?.pricingMode ?? 'PerUnit',
       length: undefined,
       width: undefined,
       thickness: undefined,
       density: undefined,
       defaultPrice: item?.defaultPrice,
       costPrice: item?.costPrice,
       defaultTaxRate: undefined,
       note: '',
       status: item ? item.status : undefined,
     };
   }

   function toCreatePayload(p: ProductFormParsed): CreateProductRequest {
     return {
       code: p.code || undefined,
       name: p.name,
       productGroupId: p.productGroupId!,
       unitId: p.unitId!,
       specification: p.specification || undefined,
       pricingMode: p.pricingMode,
       length: p.length,
       width: p.width,
       thickness: p.thickness,
       density: p.density,
       defaultPrice: p.defaultPrice,
       costPrice: p.costPrice,
       defaultTaxRate: p.defaultTaxRate,
       note: p.note || undefined,
     };
   }

   function toUpdatePayload(p: ProductFormParsed): UpdateProductRequest {
     return {
       name: p.name,
       productGroupId: p.productGroupId!,
       unitId: p.unitId!,
       specification: p.specification || undefined,
       pricingMode: p.pricingMode,
       length: p.length,
       width: p.width,
       thickness: p.thickness,
       density: p.density,
       defaultPrice: p.defaultPrice,
       costPrice: p.costPrice,
       defaultTaxRate: p.defaultTaxRate,
       note: p.note || undefined,
       status: p.status ?? 'Active',
     };
   }
   ```

   **`FormField` helper** (after helper functions — same pattern as in `product-group-form-dialog.tsx`):
   ```tsx
   interface FormFieldProps {
     label: string;
     name: keyof ProductFormValues;
     type?: string;
     hint?: string;
     form: ReturnType<typeof useForm<ProductFormValues, unknown, ProductFormParsed>>;
   }

   function FormField({ label, name, type = 'text', hint, form }: FormFieldProps) {
     const error = form.formState.errors[name as keyof typeof form.formState.errors];
     return (
       <div className="space-y-2">
         <Label htmlFor={String(name)}>{label}</Label>
         <Input id={String(name)} type={type} {...form.register(name)} />
         {hint && !error && <p className="text-xs text-muted-foreground">{hint}</p>}
         {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
       </div>
     );
   }
   ```

   > **Note on `toDefaults` for edit mode:** `ProductListItem` does not include `productGroupId` or `unitId` — only their names. The form defaults these to empty string, which means in edit mode the user is required to re-select group and unit. This is acceptable for now; the `/products` page can use the full `Product` type later if needed.

4. Run tests: `cd frontend && npx vitest run src/features/products/components/product-form-dialog.test.tsx`
   Expected: All 6 tests PASS

5. Run type check: `cd frontend && npx tsc --noEmit`
   Expected: No new errors

6. Commit:
   ```
   git add frontend/src/features/products/components/product-form-dialog.tsx \
           frontend/src/features/products/components/product-form-dialog.test.tsx
   git commit -m "feat: add ProductFormDialog — reusable product create/edit form"
   ```

## Verification

```bash
cd frontend
npx vitest run src/features/products/components/product-form-dialog.test.tsx
npx tsc --noEmit
```

## Exit Criteria

- All 6 unit tests pass
- TypeScript reports no new errors
- `ProductFormDialog` is importable as a named export from `@/features/products/components/product-form-dialog`
