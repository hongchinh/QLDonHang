import { formatNumber, formatVnd } from '../format';

export interface TopListItem {
  id: string;
  name: string;
  primary: number;
  secondary?: number | string;
}

interface TopListCardProps {
  items: TopListItem[];
  format?: 'currency' | 'number';
  loading?: boolean;
  empty?: string;
  secondaryLabel?: (value: number | string) => string;
}

export function TopListCard({ items, format = 'currency', loading, empty, secondaryLabel }: TopListCardProps) {
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
    return <p className="text-sm text-muted-foreground">{empty ?? 'Chưa có dữ liệu.'}</p>;
  }

  return (
    <ul className="divide-y divide-border">
      {items.map((it) => {
        const initial = (it.name || '?').trim().charAt(0).toUpperCase();
        const primaryDisplay = format === 'currency' ? formatVnd(it.primary) : formatNumber(it.primary);
        const secondary =
          it.secondary != null
            ? secondaryLabel
              ? secondaryLabel(it.secondary)
              : typeof it.secondary === 'number'
                ? formatNumber(it.secondary)
                : it.secondary
            : null;
        return (
          <li key={it.id} className="flex items-center gap-3 py-2">
            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-medium text-muted-foreground">
              {initial}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium text-foreground">{it.name}</p>
              {secondary && <p className="text-xs text-muted-foreground">{secondary}</p>}
            </div>
            <span className="shrink-0 text-sm font-semibold tabular-nums text-foreground">{primaryDisplay}</span>
          </li>
        );
      })}
    </ul>
  );
}
