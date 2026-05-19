import { useMemo, useState } from 'react';
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { Can } from '@/components/auth/can';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { formatVnd, formatVndShort } from '@/features/dashboard/format';
import { useVehicleRevenue } from '@/features/reports/vehicle-revenue/hooks';
import type { VehicleRevenueReport } from '@/features/reports/vehicle-revenue/types';

function startOfMonthIso(): string {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

const SERIES_COLORS = ['#2563eb', '#16a34a', '#dc2626', '#9333ea', '#ea580c', '#0891b2'];

export function VehicleRevenuePage() {
  const [from, setFrom] = useState(startOfMonthIso());
  const [to, setTo] = useState(todayIso());
  const [months, setMonths] = useState(6);

  const params = useMemo(() => ({ from, to, months }), [from, to, months]);
  const query = useVehicleRevenue(params, Boolean(from && to));

  return (
    <Can permission="reports.revenue" fallback={<div className="p-4">Bạn không có quyền xem báo cáo này.</div>}>
      <div className="space-y-4">
        <div>
          <h1 className="text-2xl font-bold">Doanh thu xe</h1>
          <p className="text-sm text-muted-foreground">
            Tổng hợp báo giá đã xác nhận theo số xe vận chuyển.
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Bộ lọc</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap items-end gap-3">
              <div>
                <Label htmlFor="vehicle-from">Từ ngày</Label>
                <Input id="vehicle-from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
              </div>
              <div>
                <Label htmlFor="vehicle-to">Đến ngày</Label>
                <Input id="vehicle-to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
              </div>
              <div className="w-32">
                <Label htmlFor="vehicle-months">Số tháng</Label>
                <Input
                  id="vehicle-months"
                  type="number"
                  min={1}
                  max={24}
                  value={months}
                  onChange={(e) => {
                    const next = Number(e.target.value);
                    if (Number.isFinite(next)) setMonths(Math.min(24, Math.max(1, next)));
                  }}
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>So sánh doanh số xe theo tháng</CardTitle>
          </CardHeader>
          <CardContent>
            <VehicleRevenueChart report={query.data} loading={query.isLoading} isError={query.isError} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Kết quả</CardTitle>
          </CardHeader>
          <CardContent>
            {query.isLoading ? (
              <div className="text-sm text-muted-foreground">Đang tải...</div>
            ) : query.isError ? (
              <div className="text-sm text-destructive">Không tải được báo cáo.</div>
            ) : !query.data || query.data.items.length === 0 ? (
              <div className="text-sm text-muted-foreground">Không có báo giá đã xác nhận trong khoảng này.</div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Số xe</TableHead>
                    <TableHead className="text-right">Số báo giá</TableHead>
                    <TableHead className="text-right">Doanh thu (gồm thuế)</TableHead>
                    <TableHead className="text-right">Doanh thu thuần</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {query.data.items.map((it) => (
                    <TableRow key={it.vehicleNumber}>
                      <TableCell>{it.vehicleNumber}</TableCell>
                      <TableCell className="text-right tabular-nums">{it.quotationCount}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatVnd(it.totalRevenueGross)}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatVnd(it.totalRevenueNet)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
                <TableFooter>
                  <TableRow>
                    <TableCell>Tổng cộng</TableCell>
                    <TableCell className="text-right tabular-nums">{query.data.totalQuotationCount}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatVnd(query.data.grandTotalGross)}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatVnd(query.data.grandTotalNet)}</TableCell>
                  </TableRow>
                </TableFooter>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>
    </Can>
  );
}

function VehicleRevenueChart({
  report,
  loading,
  isError,
}: {
  report?: VehicleRevenueReport;
  loading: boolean;
  isError: boolean;
}) {
  const rows = useMemo(() => {
    if (!report) return [];
    return report.monthlySeries.map((point) => {
      const row: Record<string, string | number> = { month: point.month };
      for (const vehicle of report.chartVehicles) row[vehicle] = 0;
      for (const value of point.values) row[value.vehicleNumber] = value.totalRevenueGross;
      return row;
    });
  }, [report]);

  const empty = !loading && (!report || report.chartVehicles.length === 0 || rows.length === 0);

  if (loading) {
    return <div className="h-72 w-full animate-pulse rounded bg-muted" />;
  }

  if (isError) {
    return <div className="text-sm text-destructive">Không tải được biểu đồ.</div>;
  }

  if (empty) {
    return (
      <div className="flex h-72 w-full items-center justify-center text-sm text-muted-foreground">
        Chưa có dữ liệu
      </div>
    );
  }

  return (
    <div className="h-72 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={rows} margin={{ top: 10, right: 16, bottom: 0, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#F0F0F0" />
          <XAxis dataKey="month" tick={{ fontSize: 11 }} stroke="#9CA3AF" />
          <YAxis tick={{ fontSize: 11 }} stroke="#9CA3AF" tickFormatter={(v: number) => formatVndShort(v)} width={72} />
          <Tooltip
            formatter={(v: number, name: string) => [formatVnd(v), name]}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
            labelStyle={{ color: '#6B7280' }}
          />
          <Legend wrapperStyle={{ fontSize: 12 }} />
          {report!.chartVehicles.map((vehicle, idx) => (
            <Line
              key={vehicle}
              type="monotone"
              dataKey={vehicle}
              stroke={SERIES_COLORS[idx % SERIES_COLORS.length]}
              strokeWidth={2}
              dot={false}
              isAnimationActive={false}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
