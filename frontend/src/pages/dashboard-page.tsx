import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export function DashboardPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Tổng quan</h1>
        <p className="text-sm text-muted-foreground">Bảng điều khiển nhanh — sẽ hoàn thiện ở các giai đoạn sau.</p>
      </div>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader>
            <CardDescription>Doanh thu hôm nay</CardDescription>
            <CardTitle>—</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Đơn hàng hôm nay</CardDescription>
            <CardTitle>—</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Đơn chờ giao</CardDescription>
            <CardTitle>—</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Tổng còn thu</CardDescription>
            <CardTitle>—</CardTitle>
          </CardHeader>
        </Card>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Module nghiệp vụ</CardTitle>
          <CardDescription>
            Các module Báo giá / Đơn hàng / Bàn giao / Thanh toán / Báo cáo sẽ được hiện thực
            dựa trên pattern của module <strong>Khách hàng</strong> ở thanh bên trái.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="list-inside list-disc text-sm text-muted-foreground">
            <li>Backend: thêm Entity → Configuration → Service → Controller → Permission → Seed</li>
            <li>Frontend: thêm <code>features/&lt;module&gt;</code> + <code>pages/&lt;module&gt;</code> + route</li>
            <li>Tham khảo: src/features/customers, src/pages/customers</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
