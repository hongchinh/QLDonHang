import { formatDistanceToNow } from 'date-fns';
import { vi } from 'date-fns/locale';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';
import type { ActivityItem } from '../types';
import { formatVnd } from '../format';

interface ActivityTimelineProps {
  items: ActivityItem[];
  loading?: boolean;
  empty?: string;
}

const TYPE_META: Record<ActivityItem['type'], { label: string; dot: string }> = {
  created: { label: 'tạo', dot: 'bg-slate-400' },
  confirmed: { label: 'xác nhận', dot: 'bg-emerald-500' },
  cancelled: { label: 'huỷ', dot: 'bg-rose-400' },
};

export function ActivityTimeline({ items, loading, empty }: ActivityTimelineProps) {
  if (loading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded bg-muted" />
        ))}
      </div>
    );
  }
  if (items.length === 0) {
    return <p className="text-sm text-muted-foreground">{empty ?? 'Chưa có hoạt động gần đây.'}</p>;
  }

  return (
    <ul className="space-y-2">
      {items.map((a, idx) => {
        const meta = TYPE_META[a.type];
        const when = formatDistanceToNow(new Date(a.at), { addSuffix: true, locale: vi });
        return (
          <li key={`${a.quotationId}-${a.type}-${idx}`} className="flex items-start gap-3 py-1.5">
            <span className={cn('mt-1.5 inline-block h-2 w-2 shrink-0 rounded-full', meta.dot)} />
            <div className="min-w-0 flex-1 text-sm">
              <div className="flex flex-wrap items-baseline gap-x-2">
                <Link
                  to={`/quotations/${a.quotationId}`}
                  className="font-medium text-foreground hover:underline"
                >
                  {a.code}
                </Link>
                <span className="text-muted-foreground">cho</span>
                <span className="truncate font-medium text-foreground">{a.customerName}</span>
              </div>
              <p className="text-xs text-muted-foreground">
                {meta.label}
                {a.actorName ? ` bởi ${a.actorName}` : ''} · {when}
                {a.amount != null && a.type !== 'created' ? ` · ${formatVnd(a.amount)}` : ''}
              </p>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
