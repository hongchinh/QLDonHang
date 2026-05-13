import { useEffect, useRef, useState } from 'react';
import { useFieldArray, useWatch, type UseFormReturn } from 'react-hook-form';
import { Plus, Trash2 } from 'lucide-react';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import type { PricingMode } from '@/features/products/types';
import type {
  QuotationFormParsed,
  QuotationFormValues,
  QuotationLineFormValues,
} from '@/features/quotations/schema';
import { ProductTypeaheadCell } from './product-typeahead-cell';
import { computeLineCost, computeLineTotal, deriveQuantityFromDimensions } from '@/pages/quotations/utils/compute-line';
import './line-items-grid.css';

const fmt = new Intl.NumberFormat('vi-VN');
const vnd = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });

const PRICING_LABEL: Record<PricingMode, string> = {
  PerUnit: 'ĐV',
  PerSquareMeter: 'm²',
  PerLinearMeter: 'm dài',
  PerCubicMeter: 'm³',
};

interface Props {
  form: UseFormReturn<QuotationFormValues, unknown, QuotationFormParsed>;
}

export function LineItemsGrid({ form }: Props) {
  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'lines',
  });
  const watched = useWatch({ control: form.control, name: 'lines' }) as QuotationLineFormValues[] | undefined;
  const rows = watched ?? [];
  const wrapRef = useRef<HTMLDivElement>(null);
  const [activeRowIndex, setActiveRowIndex] = useState<number | null>(null);
  const [clearAllOpen, setClearAllOpen] = useState(false);

  const setLineField = <K extends keyof QuotationLineFormValues>(
    idx: number,
    field: K,
    value: QuotationLineFormValues[K],
  ) => {
    form.setValue(`lines.${idx}.${field}` as const, value as never, { shouldDirty: true });
  };

  const addLine = () => {
    append({
      sortOrder: fields.length,
      productName: '',
      unitName: '',
      pricingMode: 'PerUnit',
      quantity: 1,
      unitPrice: 0,
    } as QuotationLineFormValues);
    // Auto-focus first input of the newly appended row so subsequent Insert/Ctrl+Delete
    // keystrokes reach the wrap-scoped listener (especially right after empty-state add).
    setTimeout(() => {
      const wrapEl = wrapRef.current;
      if (!wrapEl) return;
      const bodyRows = wrapEl.querySelectorAll('tbody tr');
      const lastRow = bodyRows[bodyRows.length - 1];
      const firstInput = lastRow?.querySelector<HTMLInputElement>('input');
      firstInput?.focus();
    }, 0);
  };

  useEffect(() => {
    const el = wrapRef.current;
    if (!el) return;
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === 'Insert' && !e.ctrlKey && !e.shiftKey && !e.altKey) {
        e.preventDefault();
        addLine();
        return;
      }
      if (e.key === 'Delete' && e.ctrlKey) {
        e.preventDefault();
        if (activeRowIndex != null && activeRowIndex < fields.length) {
          remove(activeRowIndex);
          setActiveRowIndex(null);
        }
      }
    }
    el.addEventListener('keydown', onKeyDown);
    return () => el.removeEventListener('keydown', onKeyDown);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeRowIndex, fields.length]);

  const subtotal = rows.reduce((sum, line) => sum + computeLineTotal(toLineLike(line)), 0);

  function clearAllLines() {
    for (let i = fields.length - 1; i >= 0; i--) remove(i);
    setActiveRowIndex(null);
    setClearAllOpen(false);
  }

  function handleClearAll() {
    if (fields.length === 0) return;
    setClearAllOpen(true);
  }

  return (
    <div className="space-y-2">
      <div ref={wrapRef} className="accounting-grid-wrap" tabIndex={-1}>
        <table className="accounting-grid">
          <colgroup>
            <col style={{ width: 46 }} />
            <col style={{ width: 190 }} />
            <col />
            <col style={{ width: 80 }} />
            <col style={{ width: 70 }} />
            <col style={{ width: 240 }} />
            <col style={{ width: 96 }} />
            <col style={{ width: 110 }} />
            <col style={{ width: 110 }} />
            <col style={{ width: 130 }} />
            <col style={{ width: 46 }} />
          </colgroup>
          <thead>
            <tr>
              <th className="row-no">#</th>
              <th>Mã hàng</th>
              <th>Tên hàng</th>
              <th>ĐVT</th>
              <th>Loại</th>
              <th>D × R × Dày × Tấm</th>
              <th>SL</th>
              <th>Đơn giá</th>
              <th>Giá vốn</th>
              <th>Thành tiền</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {fields.map((field, idx) => {
              const line = rows[idx] ?? (field as unknown as QuotationLineFormValues);
              const lineTotal = computeLineTotal(toLineLike(line));
              const lineCost = computeLineCost(toLineLike(line));
              return (
                <tr key={field.id} onFocus={() => setActiveRowIndex(idx)}>
                  <td className="row-no">{idx + 1}</td>
                  <td>
                    <ProductTypeaheadCell
                      variant="cell"
                      nextFocusId={`line-name-${idx}`}
                      value={(line.productCode ?? '') as string}
                      onChange={(v) => setLineField(idx, 'productCode', v)}
                      onSelect={(s) => {
                        setLineField(idx, 'productId', s.id);
                        setLineField(idx, 'productCode', s.code);
                        setLineField(idx, 'productName', s.name);
                        setLineField(idx, 'specification', (s.specification ?? '') as never);
                        setLineField(idx, 'unitName', (s.unitName ?? line.unitName) as never);
                        setLineField(idx, 'pricingMode', s.pricingMode);
                        setLineField(idx, 'unitPrice', (s.defaultPrice ?? 0) as never);
                        setLineField(idx, 'unitCost', s.costPrice as never);
                      }}
                    />
                  </td>
                  <td>
                    <input
                      id={`line-name-${idx}`}
                      className="cell-input"
                      aria-label="Tên hàng"
                      value={(line.productName ?? '') as string}
                      onChange={(e) => setLineField(idx, 'productName', e.target.value)}
                    />
                  </td>
                  <td>
                    <input
                      className="cell-input"
                      aria-label="Đơn vị tính"
                      value={(line.unitName ?? '') as string}
                      onChange={(e) => setLineField(idx, 'unitName', e.target.value)}
                    />
                  </td>
                  <td className="cell-pricing-mode">
                    {PRICING_LABEL[line.pricingMode] ?? line.pricingMode}
                  </td>
                  <td>
                    <div className="dxr-cell">
                      <input
                        className="cell-input"
                        type="number"
                        step="any"
                        placeholder="D"
                        aria-label="Dài"
                        value={numInput(line.length)}
                        onChange={(e) => {
                          const v = parseNum(e.target.value);
                          setLineField(idx, 'length', v as never);
                          recomputeQty(idx, { ...line, length: v });
                        }}
                      />
                      <input
                        className="cell-input"
                        type="number"
                        step="any"
                        placeholder="R"
                        aria-label="Rộng"
                        value={numInput(line.width)}
                        onChange={(e) => {
                          const v = parseNum(e.target.value);
                          setLineField(idx, 'width', v as never);
                          recomputeQty(idx, { ...line, width: v });
                        }}
                      />
                      <input
                        className="cell-input"
                        type="number"
                        step="any"
                        placeholder="Dày"
                        aria-label="Dày"
                        value={numInput(line.thickness)}
                        onChange={(e) => {
                          const v = parseNum(e.target.value);
                          setLineField(idx, 'thickness', v as never);
                          recomputeQty(idx, { ...line, thickness: v });
                        }}
                      />
                      <input
                        className="cell-input"
                        type="number"
                        step="any"
                        placeholder="Tấm"
                        aria-label="Số tấm"
                        value={numInput(line.sheetCount)}
                        onChange={(e) => {
                          const v = parseNum(e.target.value);
                          setLineField(idx, 'sheetCount', v as never);
                          recomputeQty(idx, { ...line, sheetCount: v });
                        }}
                      />
                    </div>
                  </td>
                  <td className="cell-number">
                    <input
                      className="cell-input cell-number"
                      type="number"
                      step="any"
                      aria-label="Số lượng"
                      value={numInput(line.quantity)}
                      onChange={(e) => setLineField(idx, 'quantity', (parseNum(e.target.value) ?? 0) as never)}
                    />
                  </td>
                  <td className="cell-number">
                    <input
                      className="cell-input cell-number"
                      type="number"
                      step="any"
                      aria-label="Đơn giá"
                      value={numInput(line.unitPrice)}
                      onChange={(e) => setLineField(idx, 'unitPrice', (parseNum(e.target.value) ?? 0) as never)}
                    />
                  </td>
                  <td className="cell-number">
                    <input
                      className="cell-input cell-number"
                      type="number"
                      step="any"
                      aria-label="Giá vốn"
                      value={numInput(line.unitCost)}
                      onChange={(e) => setLineField(idx, 'unitCost', parseNum(e.target.value) as never)}
                    />
                  </td>
                  <td className="cell-number">
                    <div className="cell-total-stack">
                      <div className="cell-total-main tabular-nums">{fmt.format(lineTotal)}</div>
                      {lineCost != null && (
                        <div className="cell-total-meta tabular-nums">LN: {fmt.format(lineTotal - lineCost)}</div>
                      )}
                    </div>
                  </td>
                  <td className="cell-action">
                    <button
                      type="button"
                      aria-label="Xóa dòng"
                      onClick={() => {
                        remove(idx);
                        if (activeRowIndex === idx) setActiveRowIndex(null);
                      }}
                    >
                      <Trash2 className="h-4 w-4" style={{ display: 'inline-block', verticalAlign: 'middle' }} />
                    </button>
                  </td>
                </tr>
              );
            })}
            {fields.length === 0 && (
              <tr>
                <td colSpan={11} className="empty-placeholder">
                  Chưa có dòng nào.{' '}
                  <button type="button" className="empty-placeholder-link" onClick={addLine}>
                    Bấm để thêm dòng đầu tiên
                  </button>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="line-items-footer">
        <div className="keyboard-guide">
          <span><span className="kbd">Insert</span> Thêm dòng</span>
          <span><span className="kbd">Ctrl</span>+<span className="kbd">Delete</span> Xóa dòng</span>
        </div>
        <strong>Tổng: {vnd.format(subtotal)}</strong>
      </div>

      <div className="line-items-toolbar">
        <button type="button" className="lib-btn" onClick={addLine}>
          <Plus className="h-4 w-4" />
          Thêm dòng
        </button>
        <button type="button" className="lib-btn lib-btn-danger" onClick={handleClearAll} disabled={fields.length === 0}>
          <Trash2 className="h-4 w-4" />
          Xóa tất cả dòng
        </button>
      </div>

      <ConfirmDialog
        open={clearAllOpen}
        onOpenChange={setClearAllOpen}
        title="Xóa tất cả dòng?"
        description={`Xóa toàn bộ ${fields.length} dòng?`}
        confirmLabel="Xóa"
        cancelLabel="Hủy"
        destructive
        onConfirm={clearAllLines}
      />
    </div>
  );

  function recomputeQty(idx: number, lineDraft: QuotationLineFormValues) {
    const derived = deriveQuantityFromDimensions(toLineLike(lineDraft));
    if (derived !== undefined) {
      setLineField(idx, 'quantity', derived as never);
    }
  }
}

function toLineLike(line: QuotationLineFormValues) {
  return {
    pricingMode: line.pricingMode,
    length: toNum(line.length),
    width: toNum(line.width),
    thickness: toNum(line.thickness),
    density: toNum(line.density),
    sheetCount: toNum(line.sheetCount),
    quantity: toNum(line.quantity) ?? 0,
    unitPrice: toNum(line.unitPrice) ?? 0,
    unitCost: toNum(line.unitCost),
  };
}

function toNum(v: unknown): number | undefined {
  if (v === undefined || v === null || v === '') return undefined;
  const n = typeof v === 'number' ? v : Number(v);
  return Number.isFinite(n) ? n : undefined;
}

function parseNum(v: string): number | undefined {
  if (v === '') return undefined;
  const n = Number(v);
  return Number.isFinite(n) ? n : undefined;
}

function numInput(v: unknown): string | number {
  if (v === undefined || v === null) return '';
  return v as number | string;
}
