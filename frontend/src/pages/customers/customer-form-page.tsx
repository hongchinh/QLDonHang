import { useNavigate, useParams } from 'react-router-dom';
import { useCreateCustomer, useCustomer, useUpdateCustomer } from '@/features/customers/hooks';
import { getErrorMessage } from '@/lib/api-client';
import { toast } from '@/lib/use-toast';
import { CustomerFormFields } from './customer-form-fields';

export function CustomerFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id && id !== 'new';
  const navigate = useNavigate();

  const { data: customer, isLoading } = useCustomer(isEdit ? id : undefined);
  const create = useCreateCustomer();
  const update = useUpdateCustomer();

  if (isEdit && isLoading) {
    return <div className="text-sm text-muted-foreground">Đang tải...</div>;
  }

  return (
    <CustomerFormFields
      isEdit={isEdit}
      initial={customer}
      submitting={create.isPending || update.isPending}
      submitError={getErrorMessage(create.error ?? update.error)}
      hasSubmitError={create.isError || update.isError}
      onCancel={() => navigate('/customers')}
      onSubmit={async (parsed) => {
        try {
          if (isEdit && id) {
            await update.mutateAsync({ id, data: parsed });
            toast({ variant: 'success', title: 'Đã cập nhật khách hàng' });
          } else {
            await create.mutateAsync(parsed);
            toast({ variant: 'success', title: 'Đã tạo khách hàng' });
          }
          navigate('/customers');
        } catch (err) {
          toast({ variant: 'destructive', title: 'Không thể lưu', description: getErrorMessage(err) });
        }
      }}
    />
  );
}
