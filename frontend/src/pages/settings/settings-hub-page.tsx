import { Link } from 'react-router-dom';
import { Settings, UserCog, Users2 } from 'lucide-react';
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuthStore } from '@/stores/auth-store';

export function SettingsHubPage() {
  const hasPermission = useAuthStore((s) => s.hasPermission);
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Cấu hình</h1>
        <p className="text-sm text-muted-foreground">
          Điểm vào cho các cài đặt cá nhân và quản trị.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {hasPermission('system.manage_settings') && (
          <Link to="/settings/quotation" className="block">
            <Card className="h-full transition hover:border-primary">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Settings className="h-5 w-5" />
                  <CardTitle>Cấu hình hệ thống báo giá</CardTitle>
                </div>
                <CardDescription>
                  Chọn trường ngày dùng trong báo cáo doanh thu và dashboard.
                </CardDescription>
              </CardHeader>
            </Card>
          </Link>
        )}
        <Link to="/settings/my-quotation-settings" className="block">
          <Card className="h-full transition hover:border-primary">
            <CardHeader>
              <div className="flex items-center gap-2">
                <UserCog className="h-5 w-5" />
                <CardTitle>Cài đặt báo giá của tôi</CardTitle>
              </div>
              <CardDescription>
                Upload template Excel cá nhân, xem cấu hình khoá trạng thái.
              </CardDescription>
            </CardHeader>
          </Card>
        </Link>

        {hasPermission('user_settings.manage') && (
          <Link to="/admin/users" className="block">
            <Card className="h-full transition hover:border-primary">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Users2 className="h-5 w-5" />
                  <CardTitle>Quản lý người dùng</CardTitle>
                </div>
                <CardDescription>
                  Cấu hình lock-at theo từng user và chuyển nhượng báo giá hàng loạt.
                </CardDescription>
              </CardHeader>
            </Card>
          </Link>
        )}
      </div>
    </div>
  );
}
