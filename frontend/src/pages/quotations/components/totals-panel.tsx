import { useState, type ReactNode } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { computeTotals, type HeaderLike, type LineLike } from '@/pages/quotations/utils/compute-line';
import { formatMoneyForDisplay, parseMoneyInput } from '@/pages/quotations/utils/money-input';
import { useAuthStore } from '@/stores/auth-store';

interface Props {
  lines: LineLike[];
  header: HeaderLike;
  onHeaderChange: (patch: Partial<HeaderLike>) => void;
}

const fmt = new Intl.NumberFormat('vi-VN');

export function TotalsPanel({ lines, header, onHeaderChange }: Props) {
  const totals = computeTotals(lines, header);
  const canViewCost = useAuthStore((s) => s.hasPermission('quotations.view_cost'));

  return (
    <Card className="h-full flex flex-col">
      <CardHeader><CardTitle>Tổng cộng</CardTitle></CardHeader>
      <CardContent className="flex flex-1 flex-col justify-between gap-3">
        <SummaryRow label="Tiền hàng">
          <MetricValue value={fmt.format(totals.subtotal)} />
        </SummaryRow>

        <SummaryRow label="Điều chỉnh">
          <div className="grid min-w-0 grid-cols-2 gap-1">
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
          <div className="grid min-w-0 grid-cols-[56px_1fr] items-end gap-2">
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
            {canViewCost && (
              <div className="mt-1 flex flex-wrap justify-end gap-x-3 gap-y-1 text-xs text-muted-foreground">
                <span className="tabular-nums">Giá vốn: {fmt.format(totals.totalCost)}</span>
                <span className="tabular-nums">LN gộp: {fmt.format(totals.grossProfit)}</span>
              </div>
            )}
          </div>
        </SummaryRow>

        <SummaryRow label="Tạm ứng">
          <EditableMetric
            id="advancePayment"
            value={header.advancePayment}
            onChange={(value) => onHeaderChange({ advancePayment: value })}
          />
        </SummaryRow>

        {header.advancePayment > 0 && (
          <SummaryRow label="Còn lại" emphasized>
            <div className="min-w-0 text-right">
              <MetricValue
                value={fmt.format(totals.remainingBalance)}
                bold
                large
                negative={totals.remainingBalance < 0}
              />
            </div>
          </SummaryRow>
        )}
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
    <div className={['grid grid-cols-[64px_1fr] items-center gap-2', emphasized ? 'border-t pt-3' : ''].join(' ')}>
      <span className={emphasized ? 'text-sm font-medium whitespace-nowrap' : 'text-sm text-muted-foreground whitespace-nowrap'}>{label}</span>
      <div className="min-w-0">{children}</div>
    </div>
  );
}

interface EditableMetricProps {
  id: string;
  label?: string;
  value: number;
  onChange: (value: number) => void;
}

function EditableMetric({ id, label, value, onChange }: EditableMetricProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState('');
  return (
    <div className="min-w-0">
      {label && (
        <Label htmlFor={id} className="mb-1 block text-xs text-muted-foreground">
          {label}
        </Label>
      )}
      <Input
        id={id}
        type="text"
        inputMode="decimal"
        autoComplete="off"
        value={editing ? draft : formatMoneyForDisplay(value)}
        onFocus={(e) => {
          setDraft(String(value ?? ''));
          setEditing(true);
          e.currentTarget.select();
        }}
        onBlur={() => setEditing(false)}
        onChange={(e) => {
          setDraft(e.target.value);
          const parsed = parseMoneyInput(e.target.value);
          onChange(parsed ?? 0);
        }}
        className="h-8 px-1.5 text-right tabular-nums"
      />
    </div>
  );
}

interface MetricValueProps {
  value: string;
  bold?: boolean;
  large?: boolean;
  negative?: boolean;
}

function MetricValue({ value, bold, large, negative }: MetricValueProps) {
  return (
    <span
      className={[
        'block truncate tabular-nums',
        bold ? 'font-bold' : '',
        large ? 'text-base' : 'text-sm',
        negative ? 'text-destructive' : '',
      ].join(' ')}
    >
      {value}
    </span>
  );
}
