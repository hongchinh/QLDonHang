import { Badge, type BadgeProps } from '@/components/ui/badge';
import type { QuotationStatus } from '@/features/quotations/types';

const MAP: Record<QuotationStatus, { label: string; variant: BadgeProps['variant'] }> = {
  Draft: { label: 'Nháp', variant: 'secondary' },
  Sent: { label: 'Đã gửi', variant: 'outline' },
  Confirmed: { label: 'Đã xác nhận', variant: 'success' },
  Cancelled: { label: 'Đã hủy', variant: 'destructive' },
};

export function StatusPill({ status }: { status: QuotationStatus }) {
  const m = MAP[status] ?? { label: status, variant: 'outline' as const };
  return <Badge variant={m.variant}>{m.label}</Badge>;
}
