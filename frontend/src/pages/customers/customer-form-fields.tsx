import { Controller, useForm, type UseFormReturn } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import {
  customerSchema,
  type CustomerFormParsed,
  type CustomerFormValues,
} from '@/features/customers/schema';
import type { Customer } from '@/features/customers/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';

const groups = [
  { value: 'Company', label: 'Công ty' },
  { value: 'Agent', label: 'Đại lý' },
  { value: 'Retail', label: 'Khách lẻ' },
  { value: 'Project', label: 'Công trình' },
] as const;

const statuses = [
  { value: 'Active', label: 'Đang sử dụng' },
  { value: 'Inactive', label: 'Ngừng sử dụng' },
] as const;

export interface CustomerFormFieldsProps {
  isEdit: boolean;
  initial?: Customer;
  submitting: boolean;
  submitError: string;
  hasSubmitError: boolean;
  onSubmit: (parsed: CustomerFormParsed) => Promise<void> | void;
  onCancel: () => void;
  submitLabel?: string;
  cancelLabel?: string;
  showStatusField?: boolean;
  showHeader?: boolean;
}

export function CustomerFormFields({
  isEdit,
  initial,
  submitting,
  submitError,
  hasSubmitError,
  onSubmit,
  onCancel,
  submitLabel,
  cancelLabel = 'Hủy',
  showStatusField = isEdit,
  showHeader = true,
}: CustomerFormFieldsProps) {
  const form = useForm<CustomerFormValues, unknown, CustomerFormParsed>({
    resolver: zodResolver(customerSchema),
    defaultValues: toFormDefaults(initial),
  });

  const finalSubmitLabel = submitLabel ?? (isEdit ? 'Cập nhật' : 'Tạo mới');

  return (
    <div className="space-y-4">
      {showHeader && (
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" asChild aria-label="Quay lại">
            <Link to="/customers"><ArrowLeft className="h-4 w-4" /></Link>
          </Button>
          <h1 className="text-2xl font-bold">{isEdit ? 'Chỉnh sửa khách hàng' : 'Thêm khách hàng'}</h1>
        </div>
      )}

      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <Card>
          <CardHeader><CardTitle>Thông tin chung</CardTitle></CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <Field label="Mã khách hàng" hint="Để trống để tự sinh" name="code" form={form} />
            <Field label="Tên khách hàng *" name="name" form={form} />
            <Field label="Mã số thuế" name="taxCode" form={form} />
            <Field label="Số điện thoại" name="phoneNumber" form={form} />
            <Field label="Email" name="email" type="email" form={form} />
            <div className="space-y-2">
              <Label htmlFor="group">Nhóm khách hàng</Label>
              <Controller
                control={form.control}
                name="group"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="group" aria-label="Nhóm khách hàng">
                      <SelectValue placeholder="Chọn nhóm" />
                    </SelectTrigger>
                    <SelectContent>
                      {groups.map((g) => (
                        <SelectItem key={g.value} value={g.value}>{g.label}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>
            <Field label="Người liên hệ" name="contactPerson" form={form} className="md:col-span-2" />
            <Field
              label="Địa chỉ công ty"
              name="companyAddress"
              form={form}
              className="md:col-span-2"
              multiline
            />
            <Field
              label="Địa chỉ giao hàng mặc định"
              name="defaultShippingAddress"
              form={form}
              className="md:col-span-2"
              multiline
            />
            <Field label="Ghi chú" name="note" form={form} className="md:col-span-2" multiline />

            {showStatusField && (
              <div className="space-y-2">
                <Label htmlFor="status">Trạng thái</Label>
                <Controller
                  control={form.control}
                  name="status"
                  render={({ field }) => (
                    <Select value={field.value ?? 'Active'} onValueChange={field.onChange}>
                      <SelectTrigger id="status" aria-label="Trạng thái">
                        <SelectValue placeholder="Chọn trạng thái" />
                      </SelectTrigger>
                      <SelectContent>
                        {statuses.map((s) => (
                          <SelectItem key={s.value} value={s.value}>{s.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
            )}
          </CardContent>
        </Card>

        {hasSubmitError && (
          <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
            {submitError}
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={onCancel}>
            {cancelLabel}
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Đang lưu...' : finalSubmitLabel}
          </Button>
        </div>
      </form>
    </div>
  );
}

function toFormDefaults(customer?: Customer): CustomerFormValues {
  return {
    code: customer?.code ?? '',
    name: customer?.name ?? '',
    taxCode: customer?.taxCode ?? '',
    companyAddress: customer?.companyAddress ?? '',
    defaultShippingAddress: customer?.defaultShippingAddress ?? '',
    contactPerson: customer?.contactPerson ?? '',
    phoneNumber: customer?.phoneNumber ?? '',
    email: customer?.email ?? '',
    group: customer?.group ?? 'Company',
    note: customer?.note ?? '',
    status: customer?.status ?? 'Active',
  };
}

interface FieldProps {
  label: string;
  name:
    | 'code'
    | 'name'
    | 'taxCode'
    | 'companyAddress'
    | 'defaultShippingAddress'
    | 'contactPerson'
    | 'phoneNumber'
    | 'email'
    | 'note';
  type?: string;
  hint?: string;
  className?: string;
  multiline?: boolean;
  form: UseFormReturn<CustomerFormValues, unknown, CustomerFormParsed>;
}

function Field({ label, name, type = 'text', hint, className, multiline, form }: FieldProps) {
  const error = form.formState.errors[name];
  return (
    <div className={cn('space-y-2', className)}>
      <Label htmlFor={name}>{label}</Label>
      {multiline ? (
        <Textarea id={name} rows={3} {...form.register(name)} />
      ) : (
        <Input id={name} type={type} {...form.register(name)} />
      )}
      {hint && !error && <p className="text-xs text-muted-foreground">{hint}</p>}
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}
