import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { useBulkTransfer } from '@/features/admin-user-settings/hooks';

export function BulkTransferPage() {
  const { userId } = useParams<{ userId: string }>();
  const [toUserId, setToUserId] = useState('');
  const [includeCancelled, setIncludeCancelled] = useState(false);
  const [reason, setReason] = useState('');
  const bulk = useBulkTransfer(userId ?? '');

  if (!userId) return <div>Thiếu userId trong URL.</div>;

  const handleSubmit = async () => {
    try {
      const result = await bulk.mutateAsync({ toUserId, includeCancelled, reason: reason || undefined });
      toast({
        title: 'Đã chuyển báo giá',
        description: `Đã chuyển ${result.affectedCount} báo giá.`,
      });
      setToUserId('');
      setReason('');
    } catch (err) {
      toast({ title: 'Chuyển thất bại', description: getErrorMessage(err), variant: 'destructive' });
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Chuyển toàn bộ báo giá</h1>
        <p className="text-sm text-muted-foreground">
          Từ user <code>{userId}</code> sang user khác. Dùng khi user nghỉ việc.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Thông tin chuyển</CardTitle>
          <CardDescription>
            Toàn bộ báo giá đang active của user nguồn sẽ được chuyển cho user nhận. Hành động có thể gọi lại an toàn (idempotent).
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="toUserId">User ID nhận</Label>
            <Input
              id="toUserId"
              placeholder="GUID của user nhận"
              value={toUserId}
              onChange={(e) => setToUserId(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="reason">Lý do (tuỳ chọn)</Label>
            <Input
              id="reason"
              placeholder="vd: User A nghỉ việc"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={includeCancelled}
              onChange={(e) => setIncludeCancelled(e.target.checked)}
            />
            Bao gồm báo giá đã huỷ
          </label>
          <Button onClick={handleSubmit} disabled={!toUserId || bulk.isPending}>
            Chuyển nhượng
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
