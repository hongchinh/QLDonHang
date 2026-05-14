import { useRef, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { useDeleteTemplate, useMySettings, useUploadTemplate } from '@/features/me-settings/hooks';
import { meSettingsApi } from '@/features/me-settings/api';

const MAX_BYTES = 5 * 1024 * 1024;

const LOCK_LABELS: Record<string, string> = {
  Sent: 'Đã gửi',
  Confirmed: 'Đã xác nhận',
  ConvertedToOrder: 'Đã chuyển đơn hàng',
};

export function MyQuotationSettingsPage() {
  const { data: settings, isLoading } = useMySettings();
  const upload = useUploadTemplate();
  const remove = useDeleteTemplate();
  const inputRef = useRef<HTMLInputElement>(null);
  const [downloading, setDownloading] = useState(false);

  const handlePick = () => inputRef.current?.click();

  const handleUpload = async (file: File) => {
    if (file.size > MAX_BYTES) {
      toast({ title: 'File quá lớn', description: 'Tối đa 5MB.', variant: 'destructive' });
      return;
    }
    try {
      await upload.mutateAsync(file);
      toast({ title: 'Đã tải template lên', description: file.name });
    } catch (err) {
      toast({ title: 'Tải lên thất bại', description: getErrorMessage(err), variant: 'destructive' });
    } finally {
      if (inputRef.current) inputRef.current.value = '';
    }
  };

  const handleDelete = async () => {
    try {
      await remove.mutateAsync();
      toast({ title: 'Đã xoá template', description: 'Sẽ dùng template mặc định.' });
    } catch (err) {
      toast({ title: 'Xoá thất bại', description: getErrorMessage(err), variant: 'destructive' });
    }
  };

  const handleDownload = async () => {
    if (!settings?.templateFileName) return;
    setDownloading(true);
    try {
      const blob = await meSettingsApi.downloadTemplate();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = settings.templateOriginalName ?? settings.templateFileName;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      toast({ title: 'Tải về thất bại', description: getErrorMessage(err), variant: 'destructive' });
    } finally {
      setDownloading(false);
    }
  };

  if (isLoading) return <div>Đang tải…</div>;

  const hasTemplate = !!settings?.templateFileName;
  const lockLabel = settings?.lockAtStatus ? LOCK_LABELS[settings.lockAtStatus] : null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Cài đặt báo giá của tôi</h1>
        <p className="text-sm text-muted-foreground">
          Quản lý template Excel và xem cấu hình khoá trạng thái do quản trị viên thiết lập.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Template Excel</CardTitle>
          <CardDescription>
            File .xlsx dùng khi xuất báo giá. Nếu chưa upload, hệ thống dùng template mặc định.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {hasTemplate ? (
            <>
              <p className="text-sm">
                <strong>{settings!.templateOriginalName ?? settings!.templateFileName}</strong>
                {settings!.templateUploadedAt && (
                  <span className="ml-2 text-muted-foreground">
                    (cập nhật {new Date(settings!.templateUploadedAt).toLocaleString('vi-VN')})
                  </span>
                )}
              </p>
              <div className="flex gap-2">
                <Button variant="outline" onClick={handleDownload} disabled={downloading}>
                  Tải về
                </Button>
                <Button variant="outline" onClick={handlePick}>Tải lên thay thế</Button>
                <Button variant="destructive" onClick={handleDelete} disabled={remove.isPending}>
                  Xoá
                </Button>
              </div>
            </>
          ) : (
            <>
              <p className="text-sm text-muted-foreground">Đang dùng template mặc định của hệ thống.</p>
              <Button onClick={handlePick}>Tải lên template riêng</Button>
            </>
          )}
          <input
            ref={inputRef}
            type="file"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) void handleUpload(file);
            }}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Khoá theo trạng thái</CardTitle>
          <CardDescription>Cấu hình do quản trị viên thiết lập (chỉ xem).</CardDescription>
        </CardHeader>
        <CardContent>
          {lockLabel ? (
            <p className="text-sm">
              Báo giá từ trạng thái <strong>{lockLabel}</strong> trở lên sẽ không cho phép bạn chỉnh sửa.
            </p>
          ) : (
            <p className="text-sm text-muted-foreground">Không có giới hạn nào (chưa cấu hình).</p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
