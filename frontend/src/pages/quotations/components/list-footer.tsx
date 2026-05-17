import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import type { QuotationListAggregates } from '@/features/quotations/types';

const currency = new Intl.NumberFormat('vi-VN');

interface ListFooterProps {
  totalItems: number;
  aggregates: QuotationListAggregates;
  page: number;
  totalPages: number;
  pageSize: number;
  pageSizeOptions: readonly number[];
  hasPrev: boolean;
  hasNext: boolean;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  loading?: boolean;
  errored?: boolean;
}

interface MoneyProps {
  value: number;
  loading?: boolean;
  errored?: boolean;
  strong?: boolean;
}

function Money({ value, loading, errored, strong }: MoneyProps) {
  if (loading) {
    return (
      <span
        className="inline-block h-3 w-16 animate-pulse rounded bg-muted align-middle"
        aria-label="đang tải"
      />
    );
  }
  const text = errored ? '—' : currency.format(value);
  return (
    <span className={`tabular-nums ${strong ? 'font-semibold text-foreground' : ''}`}>
      {text}
    </span>
  );
}

export function ListFooter({
  totalItems,
  aggregates,
  page,
  totalPages,
  pageSize,
  pageSizeOptions,
  hasPrev,
  hasNext,
  onPageChange,
  onPageSizeChange,
  loading,
  errored,
}: ListFooterProps) {
  const displayPages = Math.max(totalPages, 1);
  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t pt-3 text-sm">
      <div className="text-muted-foreground" role="group" aria-label="Tổng kết báo giá">
        Tổng <strong className="text-foreground">{totalItems}</strong> báo giá
        {' • '}Tiền hàng <Money value={aggregates.subtotal} loading={loading} errored={errored} />
        {' • '}CK <Money value={aggregates.discount} loading={loading} errored={errored} />
        {' • '}VC <Money value={aggregates.freight} loading={loading} errored={errored} />
        {' • '}
        <span className="font-semibold text-foreground">
          Tổng <Money value={aggregates.total} loading={loading} errored={errored} strong />
        </span>
      </div>
      <div className="flex items-center gap-2">
        <Select value={String(pageSize)} onValueChange={(v) => onPageSizeChange(Number(v))}>
          <SelectTrigger className="w-20" aria-label="Số dòng mỗi trang">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {pageSizeOptions.map((s) => (
              <SelectItem key={s} value={String(s)}>
                {s}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <span className="text-muted-foreground">
          Trang {page}/{displayPages}
        </span>
        <Button
          variant="outline"
          size="sm"
          disabled={!hasPrev}
          onClick={() => onPageChange(page - 1)}
        >
          Trước
        </Button>
        <Button
          variant="outline"
          size="sm"
          disabled={!hasNext}
          onClick={() => onPageChange(page + 1)}
        >
          Sau
        </Button>
      </div>
    </div>
  );
}
