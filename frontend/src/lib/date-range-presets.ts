import { formatDateYmd } from '@/features/dashboard/format';

export type RangePreset = '7d' | '30d' | 'this-month' | 'last-month';

export function computePreset(preset: RangePreset): { from: string; to: string } {
  const today = new Date();
  let start: Date;
  let end: Date = today;
  switch (preset) {
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
  }
  return { from: formatDateYmd(start), to: formatDateYmd(end) };
}

export function matchActivePreset(from: string, to: string): RangePreset | null {
  const presets: RangePreset[] = ['7d', '30d', 'this-month', 'last-month'];
  for (const p of presets) {
    const expected = computePreset(p);
    if (expected.from === from && expected.to === to) return p;
  }
  return null;
}
