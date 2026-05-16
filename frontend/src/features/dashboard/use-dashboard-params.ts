import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { formatDateYmd } from './format';

export type RangePreset = 'today' | '7d' | '30d' | 'this-month' | 'last-month';

export interface UseDashboardParams {
  from: string;
  to: string;
  saleUserId: string | undefined;
  setRange: (from: string, to: string) => void;
  setSaleUserId: (id: string | undefined) => void;
  setPreset: (preset: RangePreset) => void;
}

export function useDashboardParams(): UseDashboardParams {
  const [sp, setSp] = useSearchParams();

  const defaults = useMemo(() => {
    const today = new Date();
    const first = new Date(today.getFullYear(), today.getMonth(), 1);
    return { from: formatDateYmd(first), to: formatDateYmd(today) };
  }, []);

  const from = sp.get('from') ?? defaults.from;
  const to = sp.get('to') ?? defaults.to;
  const saleUserId = sp.get('saleUserId') ?? undefined;

  const setRange = useCallback(
    (f: string, t: string) => {
      setSp(
        (prev) => {
          const out = new URLSearchParams(prev);
          out.set('from', f);
          out.set('to', t);
          return out;
        },
        { replace: true },
      );
    },
    [setSp],
  );

  const setSaleUserId = useCallback(
    (id: string | undefined) => {
      setSp(
        (prev) => {
          const out = new URLSearchParams(prev);
          if (id) out.set('saleUserId', id);
          else out.delete('saleUserId');
          return out;
        },
        { replace: true },
      );
    },
    [setSp],
  );

  const setPreset = useCallback(
    (preset: RangePreset) => {
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
        case 'last-month': {
          start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
          end = new Date(today.getFullYear(), today.getMonth(), 0);
          break;
        }
        default:
          start = new Date(today.getFullYear(), today.getMonth(), 1);
      }
      setRange(formatDateYmd(start), formatDateYmd(end));
    },
    [setRange],
  );

  return { from, to, saleUserId, setRange, setSaleUserId, setPreset };
}
