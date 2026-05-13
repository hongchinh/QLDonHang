import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { computeTotals, type HeaderLike, type LineLike } from '@/pages/quotations/utils/compute-line';

interface Props {
  lines: LineLike[];
  header: HeaderLike;
  onHeaderChange: (patch: Partial<HeaderLike>) => void;
}

const fmt = new Intl.NumberFormat('vi-VN');

export function TotalsPanel({ lines, header, onHeaderChange }: Props) {
  const totals = computeTotals(lines, header);

  return (
    <Card className="h-full flex flex-col">
      <CardHeader><CardTitle>Tổng cộng</CardTitle></CardHeader>
      <CardContent className="space-y-3 flex-1 flex flex-col">
        <Row label="Cộng tiền hàng" value={fmt.format(totals.subtotal)} />

        <div className="grid grid-cols-3 items-center gap-2">
          <Label htmlFor="discount" className="col-span-1">Chiết khấu</Label>
          <Input
            id="discount"
            type="number"
            step="any"
            value={header.discount}
            onChange={(e) => onHeaderChange({ discount: Number(e.target.value) || 0 })}
            className="col-span-2 text-right tabular-nums"
          />
        </div>

        <div className="grid grid-cols-3 items-center gap-2">
          <Label htmlFor="freight" className="col-span-1">Cước vận chuyển</Label>
          <Input
            id="freight"
            type="number"
            step="any"
            value={header.freight}
            onChange={(e) => onHeaderChange({ freight: Number(e.target.value) || 0 })}
            className="col-span-2 text-right tabular-nums"
          />
        </div>

        <div className="grid grid-cols-3 items-center gap-2">
          <Label htmlFor="taxRate" className="col-span-1">Thuế suất %</Label>
          <Input
            id="taxRate"
            type="number"
            step="any"
            value={header.taxRate}
            onChange={(e) => onHeaderChange({ taxRate: Number(e.target.value) || 0 })}
            className="col-span-2 text-right tabular-nums"
          />
        </div>

        <Row label="Tiền thuế" value={fmt.format(totals.taxAmount)} />
        <div className="my-2 border-t" />
        <Row label="Tổng cộng" value={fmt.format(totals.total)} bold large />

        <div className="mt-auto space-y-3">
          <div className="border-t" />
          <Row label="Tổng giá vốn" value={fmt.format(totals.totalCost)} muted />
          <Row label="Lợi nhuận gộp" value={fmt.format(totals.grossProfit)} muted />
        </div>
      </CardContent>
    </Card>
  );
}

interface RowProps {
  label: string;
  value: string;
  bold?: boolean;
  large?: boolean;
  muted?: boolean;
}

function Row({ label, value, bold, large, muted }: RowProps) {
  return (
    <div className="flex items-center justify-between">
      <span className={muted ? 'text-sm text-muted-foreground' : 'text-sm'}>{label}</span>
      <span
        className={[
          'tabular-nums',
          bold ? 'font-bold' : '',
          large ? 'text-base' : 'text-sm',
          muted ? 'text-muted-foreground' : '',
        ].join(' ')}
      >
        {value}
      </span>
    </div>
  );
}
