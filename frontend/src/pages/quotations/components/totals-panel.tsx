import type { ReactNode } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { computeTotals, type HeaderLike, type LineLike } from '@/pages/quotations/utils/compute-line';

interface Props {
  lines: LineLike[];
  header: HeaderLike;
  onHeaderChange: (patch: Partial<HeaderLike>) => void;
}

const fmt = new Intl.NumberFormat('vi-VN');

export function TotalsPanel({ lines, header, onHeaderChange }: Props) {
  const totals = computeTotals(lines, header);

  return (
    <Card className="h-full flex flex-col">
      <CardHeader><CardTitle>Tổng cộng</CardTitle></CardHeader>
      <CardContent className="flex flex-1 flex-col justify-between gap-3">
        <SummaryRow label="Tiền hàng">
          <MetricValue value={fmt.format(totals.subtotal)} />
        </SummaryRow>

        <SummaryRow label="Điều chỉnh">
          <div className="grid min-w-0 grid-cols-2 gap-2">
            <EditableMetric
              id="discount"
              label="CK"
              value={header.discount}
              onChange={(value) => onHeaderChange({ discount: value })}
            />
            <EditableMetric
              id="freight"
              label="VC"
              value={header.freight}
              onChange={(value) => onHeaderChange({ freight: value })}
            />
          </div>
        </SummaryRow>

        <SummaryRow label="Thuế">
          <div className="grid min-w-0 grid-cols-[92px_1fr] items-end gap-2">
            <EditableMetric
              id="taxRate"
              label="Thuế %"
              value={header.taxRate}
              onChange={(value) => onHeaderChange({ taxRate: value })}
            />
            <div className="min-w-0 text-right">
              <div className="text-xs text-muted-foreground">Tiền thuế</div>
              <MetricValue value={fmt.format(totals.taxAmount)} />
            </div>
          </div>
        </SummaryRow>

        <SummaryRow label="Tổng cộng" emphasized>
          <div className="min-w-0 text-right">
            <MetricValue value={fmt.format(totals.total)} bold large />
            <div className="mt-1 flex flex-wrap justify-end gap-x-3 gap-y-1 text-xs text-muted-foreground">
              <span className="tabular-nums">Giá vốn: {fmt.format(totals.totalCost)}</span>
              <span className="tabular-nums">LN gộp: {fmt.format(totals.grossProfit)}</span>
            </div>
          </div>
        </SummaryRow>
      </CardContent>
    </Card>
  );
}

interface SummaryRowProps {
  label: string;
  children: ReactNode;
  emphasized?: boolean;
}

function SummaryRow({ label, children, emphasized }: SummaryRowProps) {
  return (
    <div className={['grid grid-cols-[86px_1fr] items-center gap-3', emphasized ? 'border-t pt-3' : ''].join(' ')}>
      <span className={emphasized ? 'text-sm font-medium' : 'text-sm text-muted-foreground'}>{label}</span>
      <div className="min-w-0">{children}</div>
    </div>
  );
}

interface EditableMetricProps {
  id: string;
  label: string;
  value: number;
  onChange: (value: number) => void;
}

function EditableMetric({ id, label, value, onChange }: EditableMetricProps) {
  return (
    <div className="min-w-0">
      <Label htmlFor={id} className="mb-1 block text-xs text-muted-foreground">
        {label}
      </Label>
      <Input
        id={id}
        type="number"
        step="any"
        value={value}
        onChange={(e) => onChange(Number(e.target.value) || 0)}
        className="h-8 px-2 text-right tabular-nums"
      />
    </div>
  );
}

interface MetricValueProps {
  value: string;
  bold?: boolean;
  large?: boolean;
}

function MetricValue({ value, bold, large }: MetricValueProps) {
  return (
    <span
      className={['block truncate tabular-nums', bold ? 'font-bold' : '', large ? 'text-base' : 'text-sm'].join(' ')}
    >
      {value}
    </span>
  );
}
