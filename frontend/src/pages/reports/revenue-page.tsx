import { useState } from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { toast } from '@/lib/use-toast';
import { useAuthStore } from '@/stores/auth-store';
import { KpiCard } from '@/features/dashboard/components/kpi-card';
import { RangePicker } from '@/features/dashboard/components/range-picker';
import { RevenueAreaChart } from '@/features/dashboard/components/revenue-area-chart';
import { TopListCard } from '@/features/dashboard/components/top-list-card';
import {
  useDashboardSummary,
  useRevenueSeries,
  useTopCustomers,
} from '@/features/dashboard/hooks';
import { useDashboardParams } from '@/features/dashboard/use-dashboard-params';
import { useAdminUsers } from '@/features/admin-users/hooks';
import type { Granularity, Kpi } from '@/features/dashboard/types';
import { formatNumber, formatVnd } from '@/features/dashboard/format';

const ALL_SALES = '__all__';

export function RevenuePage() {
  const { from, to, saleUserId, setRange, setSaleUserId, setPreset } = useDashboardParams();
  const [granularity, setGranularity] = useState<Granularity>('day');
  const isAdmin = useAuthStore((s) => s.hasPermission('quotations.view_all'));

  const summary = useDashboardSummary({ from, to, saleUserId });
  const revenue = useRevenueSeries({ from, to, granularity, saleUserId });
  const topCustomers = useTopCustomers({ from, to, limit: 5, saleUserId });
  const usersQuery = useAdminUsers({ activeOnly: true });

  const rangeRevenue: Kpi | undefined = summary.data?.rangeRevenue;
  const totalCount = summary.data?.totalCount.value ?? 0;
  const cancelledCount = summary.data?.cancelledCount.value ?? 0;
  const confirmedCount = Math.max(0, Math.round(totalCount - cancelledCount));
  const avgPerQuote = confirmedCount > 0 && rangeRevenue ? rangeRevenue.value / confirmedCount : 0;

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Báo cáo doanh thu</h1>
          <p className="text-sm text-muted-foreground">
            {summary.data ? `Từ ${summary.data.from} đến ${summary.data.to}` : 'Báo giá xác nhận trong khoảng đang xem.'}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {isAdmin && (
            <Select
              value={saleUserId ?? ALL_SALES}
              onValueChange={(v) => setSaleUserId(v === ALL_SALES ? undefined : v)}
            >
              <SelectTrigger className="w-56">
                <SelectValue placeholder="Tất cả sale" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL_SALES}>Tất cả sale</SelectItem>
                {(usersQuery.data ?? []).map((u) => (
                  <SelectItem key={u.id} value={u.id}>
                    {u.fullName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          <RangePicker from={from} to={to} onChange={setRange} onPreset={setPreset} />
          <Button
            variant="outline"
            size="sm"
            onClick={() => toast({ title: 'Sắp ra mắt', description: 'Xuất Excel đang được phát triển.' })}
          >
            Xuất Excel
          </Button>
        </div>
      </header>

      <div className="grid gap-4 md:grid-cols-3">
        <KpiCard
          label="Tổng doanh thu"
          format="currency"
          kpi={rangeRevenue}
          loading={summary.isLoading}
          accentColor="hsl(var(--primary))"
        />
        <KpiCard
          label="Số báo giá xác nhận"
          format="number"
          kpi={{ value: confirmedCount, deltaPct: null, spark: [] }}
          loading={summary.isLoading}
        />
        <KpiCard
          label="Trung bình / BG"
          format="currency"
          kpi={{ value: avgPerQuote, deltaPct: null, spark: [] }}
          loading={summary.isLoading}
        />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Doanh thu theo {granularity === 'day' ? 'ngày' : granularity === 'week' ? 'tuần' : 'tháng'}</CardTitle>
        </CardHeader>
        <CardContent>
          <RevenueAreaChart
            points={revenue.data?.points ?? []}
            granularity={granularity}
            onGranularityChange={setGranularity}
            loading={revenue.isLoading}
          />
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Bảng doanh thu theo ngày</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ngày</TableHead>
                  <TableHead className="text-right">Số BG</TableHead>
                  <TableHead className="text-right">Doanh thu</TableHead>
                  <TableHead className="text-right">TB / BG</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(revenue.data?.points ?? []).filter((p) => p.total > 0 || p.confirmedCount > 0).map((p) => (
                  <TableRow key={p.date}>
                    <TableCell>{p.date}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatNumber(p.confirmedCount)}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatVnd(p.total)}</TableCell>
                    <TableCell className="text-right tabular-nums">
                      {p.confirmedCount > 0 ? formatVnd(p.total / p.confirmedCount) : '—'}
                    </TableCell>
                  </TableRow>
                ))}
                {(revenue.data?.points ?? []).every((p) => p.total === 0 && p.confirmedCount === 0) && !revenue.isLoading && (
                  <TableRow>
                    <TableCell colSpan={4} className="text-center text-sm text-muted-foreground">
                      Chưa có dữ liệu.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Top khách hàng</CardTitle>
          </CardHeader>
          <CardContent>
            <TopListCard
              format="currency"
              loading={topCustomers.isLoading}
              items={(topCustomers.data ?? []).map((c) => ({
                id: c.customerId,
                name: c.customerName,
                primary: c.revenue,
                secondary: `${formatNumber(c.quotationCount)} BG`,
              }))}
            />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
