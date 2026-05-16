import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { useResetAdminUserPassword } from '@/features/admin-users/hooks';

const schema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Mật khẩu tối thiểu 8 ký tự.')
      .regex(/(?=.*[A-Za-z])(?=.*\d)/, 'Phải có cả chữ và số.'),
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp.',
    path: ['confirmPassword'],
  });

type Values = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  userId: string;
  username: string;
}

export function ResetPasswordDialog({ open, onOpenChange, userId, username }: Props) {
  const reset = useResetAdminUserPassword(userId);

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  });

  useEffect(() => {
    if (open) form.reset();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await reset.mutateAsync({ newPassword: values.newPassword });
      toast({
        variant: 'success',
        title: 'Đã đặt lại mật khẩu',
        description: `Người dùng ${username} sẽ bị đăng xuất khỏi mọi thiết bị.`,
      });
      onOpenChange(false);
    } catch (err) {
      toast({ variant: 'destructive', title: 'Đặt lại mật khẩu thất bại', description: getErrorMessage(err) });
    }
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Đặt lại mật khẩu — {username}</DialogTitle>
        </DialogHeader>
        <form onSubmit={onSubmit} className="space-y-3">
          <div className="space-y-1">
            <Label>Mật khẩu mới</Label>
            <Input type="password" {...form.register('newPassword')} />
            {form.formState.errors.newPassword?.message && (
              <p className="text-xs text-destructive">{form.formState.errors.newPassword.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label>Xác nhận mật khẩu</Label>
            <Input type="password" {...form.register('confirmPassword')} />
            {form.formState.errors.confirmPassword?.message && (
              <p className="text-xs text-destructive">{form.formState.errors.confirmPassword.message}</p>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Hủy
            </Button>
            <Button type="submit" disabled={reset.isPending}>
              {reset.isPending ? 'Đang lưu...' : 'Đặt lại'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
