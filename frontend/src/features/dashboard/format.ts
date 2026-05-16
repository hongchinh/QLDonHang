export function formatVnd(value: number): string {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatVndShort(value: number): string {
  const abs = Math.abs(value);
  if (abs >= 1_000_000_000) return `₫${(value / 1_000_000_000).toFixed(1)}B`;
  if (abs >= 1_000_000) return `₫${(value / 1_000_000).toFixed(1)}M`;
  if (abs >= 1_000) return `₫${(value / 1_000).toFixed(1)}K`;
  return `₫${value}`;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat('vi-VN').format(value);
}

export function formatDelta(deltaPct: number | null | undefined): {
  text: string;
  tone: 'positive' | 'negative' | 'neutral';
} {
  if (deltaPct == null || !Number.isFinite(deltaPct)) {
    return { text: '—', tone: 'neutral' };
  }
  const sign = deltaPct >= 0 ? '▲' : '▼';
  return {
    text: `${sign} ${Math.abs(deltaPct).toFixed(1)}%`,
    tone: deltaPct >= 0 ? 'positive' : 'negative',
  };
}

export function formatDateYmd(d: Date): string {
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}
