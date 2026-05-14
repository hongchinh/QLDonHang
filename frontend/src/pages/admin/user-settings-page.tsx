import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { useSetLockAt, useUserSettings } from '@/features/admin-user-settings/hooks';
import type { LockAtStatus } from '@/features/admin-user-settings/types';

const OPTIONS: { value: '' | NonNullable<LockAtStatus>; label: string }[] = [
  { value: '', label: 'Không khoá' },
  { value: 'Sent', label: 'Từ Đã gửi' },
  { value: 'Confirmed', label: 'Từ Đã xác nhận' },
  { value: 'ConvertedToOrder', label: 'Từ Đã chuyển đơn hàng' },
];

export function UserSettingsPage() {
  const { userId } = useParams<{ userId: string }>();
  const { data: settings, isLoading } = useUserSettings(userId);
  const setLockAt = useSetLockAt(userId ?? '');
  const [value, setValue] = useState<'' | NonNullable<LockAtStatus>>('');

  useEffect(() => {
    if (settings) setValue((settings.lockAtStatus ?? '') as '' | NonNullable<LockAtStatus>);
  }, [settings]);

  if (!userId) return <div>Thiếu userId trong URL.</div>;
  if (isLoading) return <div>Đang tải…</div>;
  if (!settings) return <div>Không tìm thấy user.</div>;

  const handleSave = async () => {
    try {
      await setLockAt.mutateAsync({ lockAtStatus: (value || null) as LockAtStatus });
      toast({ title: 'Đã cập nhật cấu hình khoá' });
    } catch (err) {
      toast({ title: 'Lưu thất bại', description: getErrorMessage(err), variant: 'destructive' });
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Cấu hình báo giá của user</h1>
        <p className="text-sm text-muted-foreground">
          {settings.userFullName ?? settings.userId}
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Khoá theo trạng thái</CardTitle>
          <CardDescription>
            User sẽ không sửa được báo giá khi trạng thái ≥ ngưỡng được chọn (Admin/Manager bypass).
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <select
            value={value}
            onChange={(e) => setValue(e.target.value as '' | NonNullable<LockAtStatus>)}
            className="w-full max-w-sm rounded-md border px-3 py-2"
          >
            {OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
          <Button onClick={handleSave} disabled={setLockAt.isPending}>Lưu</Button>
        </CardContent>
      </Card>
    </div>
  );
}
