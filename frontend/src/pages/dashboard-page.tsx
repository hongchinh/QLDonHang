import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useQuotationStats } from '@/features/dashboard/hooks';
import { useAuthStore } from '@/stores/auth-store';

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
}

export function DashboardPage() {
  const hasViewAll = useAuthStore((s) => s.hasPermission('quotations.view_all'));
  const { data: stats, isLoading } = useQuotationStats();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">
          {hasViewAll ? 'Tổng quan hệ thống' : 'Tổng quan của tôi'}
        </h1>
        <p className="text-sm text-muted-foreground">
          {stats
            ? `Từ ${stats.from} đến ${stats.to}`
            : 'Báo giá trong tháng hiện tại.'}
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader>
            <CardDescription>Doanh thu hôm nay</CardDescription>
            <CardTitle>{isLoading ? '—' : formatCurrency(stats?.todayRevenue ?? 0)}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Doanh thu khoảng đang xem</CardDescription>
            <CardTitle>{isLoading ? '—' : formatCurrency(stats?.totalRevenue ?? 0)}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Tổng báo giá</CardDescription>
            <CardTitle>{isLoading ? '—' : stats?.totalCount ?? 0}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Đã huỷ</CardDescription>
            <CardTitle>{isLoading ? '—' : stats?.cancelledCount ?? 0}</CardTitle>
          </CardHeader>
        </Card>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Trạng thái báo giá</CardTitle>
        </CardHeader>
        <CardContent>
          <ul className="grid gap-2 sm:grid-cols-2 md:grid-cols-4">
            <li>Nháp: <strong>{stats?.draftCount ?? 0}</strong></li>
            <li>Đã gửi: <strong>{stats?.sentCount ?? 0}</strong></li>
            <li>Đã xác nhận: <strong>{stats?.confirmedCount ?? 0}</strong></li>
            <li>Đã chuyển ĐH: <strong>{stats?.convertedCount ?? 0}</strong></li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
