import { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Input } from '@/components/ui/input';
import { useProductSearch } from '@/features/products/hooks';
import type { ProductSuggestion } from '@/features/products/types';

interface Props {
  value: string;
  onChange: (value: string) => void;
  onSelect: (s: ProductSuggestion) => void;
  placeholder?: string;
  variant?: 'default' | 'cell';
}

const PRICING_LABEL: Record<ProductSuggestion['pricingMode'], string> = {
  PerUnit: 'ĐV',
  PerSquareMeter: 'm²',
  PerLinearMeter: 'm dài',
  PerCubicMeter: 'm³',
};

const currency = new Intl.NumberFormat('vi-VN');

export function ProductTypeaheadCell({ value, onChange, onSelect, placeholder, variant = 'default' }: Props) {
  const [open, setOpen] = useState(false);
  const [activeIdx, setActiveIdx] = useState(0);
  const [dropdownRect, setDropdownRect] = useState<DOMRect | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const search = useProductSearch(value);
  const items = search.data ?? [];

  const syncDropdownRect = useCallback(() => {
    setDropdownRect(inputRef.current?.getBoundingClientRect() ?? null);
  }, []);

  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      const target = e.target as Node;
      if (containerRef.current?.contains(target)) return;
      if (dropdownRef.current?.contains(target)) return;
      setOpen(false);
    }
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, []);

  useEffect(() => {
    if (!open) return;
    syncDropdownRect();
    window.addEventListener('resize', syncDropdownRect);
    window.addEventListener('scroll', syncDropdownRect, true);
    return () => {
      window.removeEventListener('resize', syncDropdownRect);
      window.removeEventListener('scroll', syncDropdownRect, true);
    };
  }, [open, syncDropdownRect]);

  useEffect(() => {
    if (activeIdx >= items.length) setActiveIdx(0);
  }, [items.length, activeIdx]);

  const commit = (s: ProductSuggestion) => {
    onSelect(s);
    setOpen(false);
  };

  const triggerProps = {
    ref: inputRef,
    value,
    onFocus: () => {
      syncDropdownRect();
      setOpen(true);
    },
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange(e.target.value);
      syncDropdownRect();
      setOpen(true);
    },
    onKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (!open || items.length === 0) return;
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setActiveIdx((i) => Math.min(i + 1, items.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setActiveIdx((i) => Math.max(i - 1, 0));
      } else if (e.key === 'Enter') {
        e.preventDefault();
        commit(items[activeIdx]);
      } else if (e.key === 'Escape') {
        setOpen(false);
      }
    },
    placeholder: placeholder ?? 'Mã / tên hàng',
  };

  const trigger = variant === 'cell'
    ? <input className="cell-input" {...triggerProps} />
    : <Input {...triggerProps} />;

  const dropdown = open && items.length > 0 && dropdownRect
    ? createPortal(
      <div
        ref={dropdownRef}
        className="z-50 max-h-72 w-[480px] overflow-auto rounded-md border bg-popover shadow-md"
        style={{
          position: 'fixed',
          left: dropdownRect.left,
          top: dropdownRect.bottom + 4,
          maxWidth: 'calc(100vw - 24px)',
        }}
      >
        <table className="w-full text-sm">
          <thead className="text-xs text-muted-foreground">
            <tr>
              <th className="px-2 py-1 text-left">Mã</th>
              <th className="px-2 py-1 text-left">Tên</th>
              <th className="px-2 py-1 text-left">Loại</th>
              <th className="px-2 py-1 text-left">Quy cách</th>
              <th className="px-2 py-1 text-right">Giá bán</th>
            </tr>
          </thead>
          <tbody>
            {items.map((s, idx) => (
              <tr
                key={s.id}
                onMouseEnter={() => setActiveIdx(idx)}
                onMouseDown={(e) => {
                  e.preventDefault();
                  commit(s);
                }}
                className={
                  idx === activeIdx
                    ? 'cursor-pointer bg-accent text-accent-foreground'
                    : 'cursor-pointer hover:bg-accent/50'
                }
              >
                <td className="px-2 py-1 font-mono">{s.code}</td>
                <td className="px-2 py-1">{s.name}</td>
                <td className="px-2 py-1">{PRICING_LABEL[s.pricingMode]}</td>
                <td className="px-2 py-1 text-xs text-muted-foreground">{s.specification ?? ''}</td>
                <td className="px-2 py-1 text-right tabular-nums">
                  {s.defaultPrice != null ? currency.format(s.defaultPrice) : ''}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>,
      document.body,
    )
    : null;

  return (
    <div ref={containerRef} className="relative">
      {trigger}
      {dropdown}
    </div>
  );
}
