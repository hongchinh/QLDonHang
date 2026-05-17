import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Button } from '@/components/ui/button';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { useDeleteAdminRole } from '@/features/admin-roles/hooks';
import type { RoleListItem } from '@/features/admin-roles/types';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  role: RoleListItem | null;
}

export function RoleDeleteConfirm({ open, onOpenChange, role }: Props) {
  const remove = useDeleteAdminRole();

  if (!role) return null;

  const blocked = role.userCount > 0;

  if (blocked) {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Không thể xoá — {role.code}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Vai trò này đang được gán cho <span className="font-medium">{role.userCount}</span> người
            dùng. Vui lòng chuyển các user sang vai trò khác trước khi xoá.
          </p>
          <DialogFooter>
            <Button onClick={() => onOpenChange(false)}>Đã hiểu</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title={`Xoá vai trò ${role.code}?`}
      description={
        <>
          Hành động này sẽ xoá vai trò <span className="font-medium">{role.code}</span> và bỏ tất cả
          permission đã gán cho vai trò. Không thể hoàn tác.
        </>
      }
      destructive
      confirmLabel="Xoá vai trò"
      loading={remove.isPending}
      onConfirm={async () => {
        try {
          await remove.mutateAsync(role.id);
          toast({ variant: 'success', title: 'Đã xoá vai trò', description: role.code });
          onOpenChange(false);
        } catch (err) {
          toast({ variant: 'destructive', title: 'Xoá thất bại', description: getErrorMessage(err) });
        }
      }}
    />
  );
}
