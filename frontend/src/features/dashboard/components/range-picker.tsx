import { useEffect, useRef, useState } from 'react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { formatDateYmd } from '../format';
import type { RangePreset } from '../use-dashboard-params';

interface RangePickerProps {
  from: string;
  to: string;
  onChange: (from: string, to: string) => void;
  onPreset?: (preset: RangePreset) => void;
}

const PRESETS: { key: RangePreset; label: string }[] = [
  { key: 'today', label: 'Hôm nay' },
  { key: '7d', label: '7N' },
  { key: '30d', label: '30N' },
  { key: 'this-month', label: 'Tháng này' },
  { key: 'last-month', label: 'Tháng trước' },
];

export function RangePicker({ from, to, onChange, onPreset }: RangePickerProps) {
  const [open, setOpen] = useState(false);
  const [draftFrom, setDraftFrom] = useState(from);
  const [draftTo, setDraftTo] = useState(to);
  const wrapRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setDraftFrom(from);
    setDraftTo(to);
  }, [from, to]);

  useEffect(() => {
    if (!open) return;
    function handleClick(e: MouseEvent) {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  const matchPreset = matchActivePreset(from, to);

  const handlePresetClick = (preset: RangePreset) => {
    if (onPreset) onPreset(preset);
    else {
      const { from: f, to: t } = computePreset(preset);
      onChange(f, t);
    }
  };

  const handleApply = () => {
    if (draftFrom && draftTo) {
      onChange(draftFrom, draftTo);
      setOpen(false);
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-2" ref={wrapRef}>
      {PRESETS.map((p) => (
        <button
          key={p.key}
          type="button"
          onClick={() => handlePresetClick(p.key)}
          className={cn(
            'rounded-full border px-3 py-1 text-xs font-medium transition-colors',
            matchPreset === p.key
              ? 'border-foreground bg-foreground text-background'
              : 'border-border bg-card text-muted-foreground hover:bg-accent',
          )}
        >
          {p.label}
        </button>
      ))}
      <div className="relative">
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className={cn(
            'rounded-full border px-3 py-1 text-xs font-medium transition-colors',
            matchPreset == null
              ? 'border-foreground bg-foreground text-background'
              : 'border-border bg-card text-muted-foreground hover:bg-accent',
          )}
        >
          Tuỳ chỉnh ▾
        </button>
        {open && (
          <div className="absolute right-0 top-9 z-20 w-72 rounded-lg border border-border bg-popover p-3 shadow-md">
            <div className="space-y-2">
              <label className="block text-xs text-muted-foreground">
                Từ ngày
                <Input
                  type="date"
                  value={draftFrom}
                  onChange={(e) => setDraftFrom(e.target.value)}
                  className="mt-1"
                />
              </label>
              <label className="block text-xs text-muted-foreground">
                Đến ngày
                <Input
                  type="date"
                  value={draftTo}
                  onChange={(e) => setDraftTo(e.target.value)}
                  className="mt-1"
                />
              </label>
              <div className="flex justify-end gap-2 pt-1">
                <Button variant="ghost" size="sm" onClick={() => setOpen(false)}>
                  Huỷ
                </Button>
                <Button size="sm" onClick={handleApply}>
                  Áp dụng
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
      <span className="ml-1 text-xs text-muted-foreground">
        {from} → {to}
      </span>
    </div>
  );
}

function computePreset(preset: RangePreset): { from: string; to: string } {
  const today = new Date();
  let start: Date;
  let end: Date = today;
  switch (preset) {
    case 'today':
      start = new Date(today);
      break;
    case '7d':
      start = new Date(today);
      start.setDate(start.getDate() - 6);
      break;
    case '30d':
      start = new Date(today);
      start.setDate(start.getDate() - 29);
      break;
    case 'this-month':
      start = new Date(today.getFullYear(), today.getMonth(), 1);
      break;
    case 'last-month':
      start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
      end = new Date(today.getFullYear(), today.getMonth(), 0);
      break;
    default:
      start = new Date(today.getFullYear(), today.getMonth(), 1);
  }
  return { from: formatDateYmd(start), to: formatDateYmd(end) };
}

function matchActivePreset(from: string, to: string): RangePreset | null {
  for (const p of PRESETS) {
    const expected = computePreset(p.key);
    if (expected.from === from && expected.to === to) return p.key;
  }
  return null;
}
