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
