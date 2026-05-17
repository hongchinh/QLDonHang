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
import { Textarea } from '@/components/ui/textarea';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { useUpdateAdminRole } from '@/features/admin-roles/hooks';
import type { RoleListItem } from '@/features/admin-roles/types';

const schema = z.object({
  name: z.string().trim().min(1, 'Tên vai trò là bắt buộc.').max(200),
  description: z.string().trim().max(500).optional().nullable(),
});

type Values = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  role: RoleListItem | null;
}

export function RoleRenameDialog({ open, onOpenChange, role }: Props) {
  const update = useUpdateAdminRole(role?.id ?? '');

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', description: '' },
  });

  useEffect(() => {
    if (open && role) {
      form.reset({ name: role.name, description: role.description ?? '' });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, role?.id]);

  const onSubmit = form.handleSubmit(async (values) => {
    if (!role) return;
    try {
      await update.mutateAsync({
        name: values.name,
        description: values.description || null,
      });
      toast({ variant: 'success', title: 'Đã cập nhật vai trò', description: role.code });
      onOpenChange(false);
    } catch (err) {
      toast({ variant: 'destructive', title: 'Cập nhật thất bại', description: getErrorMessage(err) });
    }
  });

  if (!role) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Đổi tên — {role.code}</DialogTitle>
        </DialogHeader>
        <form onSubmit={onSubmit} className="space-y-3">
          <div className="space-y-1">
            <Label htmlFor="name">Tên hiển thị</Label>
            <Input id="name" {...form.register('name')} />
            {form.formState.errors.name && (
              <p className="text-xs text-destructive">{form.formState.errors.name.message}</p>
            )}
          </div>
          <div className="space-y-1">
            <Label htmlFor="description">Mô tả</Label>
            <Textarea id="description" rows={2} {...form.register('description')} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Hủy
            </Button>
            <Button type="submit" disabled={update.isPending}>
              {update.isPending ? 'Đang lưu…' : 'Cập nhật'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
