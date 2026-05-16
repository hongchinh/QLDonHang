import { cn } from '@/lib/utils';
import type { Funnel } from '../types';
import { formatNumber } from '../format';

interface StatusFunnelProps {
  data?: Funnel | null;
  loading?: boolean;
}

const STAGES: { key: keyof Pick<Funnel, 'draft' | 'sent' | 'confirmed' | 'cancelled'>; label: string; tone: string }[] = [
  { key: 'draft', label: 'Nháp', tone: 'bg-slate-400' },
  { key: 'sent', label: 'Đã gửi', tone: 'bg-sky-400' },
  { key: 'confirmed', label: 'Đã xác nhận', tone: 'bg-emerald-500' },
  { key: 'cancelled', label: 'Đã huỷ', tone: 'bg-rose-400' },
];

export function StatusFunnel({ data, loading }: StatusFunnelProps) {
  if (loading) {
    return (
      <div className="space-y-3">
        {STAGES.map((s) => (
          <div key={s.key} className="h-9 animate-pulse rounded bg-muted" />
        ))}
      </div>
    );
  }
  if (!data) {
    return <p className="text-sm text-muted-foreground">Chưa có dữ liệu.</p>;
  }
  const max = Math.max(data.draft, data.sent, data.confirmed, data.cancelled, 1);

  return (
    <div className="space-y-2">
      {STAGES.map((s) => {
        const value = data[s.key];
        const pct = Math.max(2, Math.round((value / max) * 100));
        return (
          <div key={s.key} className="flex items-center gap-3">
            <span className="w-28 shrink-0 text-xs text-muted-foreground">{s.label}</span>
            <div className="relative h-6 flex-1 overflow-hidden rounded bg-muted">
              <div className={cn('absolute inset-y-0 left-0', s.tone)} style={{ width: `${pct}%` }} />
            </div>
            <span className="w-10 shrink-0 text-right text-sm font-medium tabular-nums">{formatNumber(value)}</span>
          </div>
        );
      })}
      <div className="flex flex-wrap gap-x-4 gap-y-1 pt-2 text-xs text-muted-foreground">
        <span>
          Gửi/Nháp:{' '}
          <span className="font-medium text-foreground">
            {data.sentRate != null ? `${data.sentRate.toFixed(1)}%` : '—'}
          </span>
        </span>
        <span>
          Xác nhận/Gửi:{' '}
          <span className="font-medium text-foreground">
            {data.confirmRate != null ? `${data.confirmRate.toFixed(1)}%` : '—'}
          </span>
        </span>
      </div>
    </div>
  );
}
