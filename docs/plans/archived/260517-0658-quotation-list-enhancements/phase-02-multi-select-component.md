# Phase 02 — MultiSelect Component

**Status:** [x] completed
**Complexity:** S

## Objective
Tạo component generic `frontend/src/components/ui/multi-select.tsx` dùng cho mọi filter multi-value (status, customer category, v.v...). Sử dụng `DropdownMenu` + `DropdownMenuCheckboxItem` có sẵn — không thêm dep mới.

## Files
- `frontend/src/components/ui/multi-select.tsx` (mới)

## Tasks

### 1. Tạo file `multi-select.tsx`

Skeleton:
```tsx
import * as React from 'react';
import { ChevronDown } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuCheckboxItem,
  DropdownMenuSeparator,
  DropdownMenuItem,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';

export interface MultiSelectOption<T extends string> {
  value: T;
  label: string;
}

export interface MultiSelectProps<T extends string> {
  options: MultiSelectOption<T>[];
  value: T[];
  onChange: (next: T[]) => void;
  placeholder: string;
  className?: string;
  triggerClassName?: string;
  ariaLabel?: string;
}

export function MultiSelect<T extends string>({
  options,
  value,
  onChange,
  placeholder,
  className,
  triggerClassName,
  ariaLabel,
}: MultiSelectProps<T>) {
  const selectedSet = React.useMemo(() => new Set(value), [value]);

  const toggle = (v: T) => {
    const next = selectedSet.has(v)
      ? value.filter((x) => x !== v)
      : [...value, v];
    onChange(next);
  };

  const clear = () => onChange([]);

  const label =
    value.length === 0
      ? placeholder
      : `${placeholder} (${value.length})`;

  return (
    <div className={className}>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="outline"
            className={cn('justify-between gap-2', triggerClassName)}
            aria-label={ariaLabel ?? placeholder}
          >
            <span className={value.length === 0 ? 'text-muted-foreground' : ''}>
              {label}
            </span>
            <ChevronDown className="h-4 w-4 opacity-50" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-[var(--radix-dropdown-menu-trigger-width)] min-w-[10rem]">
          {options.map((opt) => (
            <DropdownMenuCheckboxItem
              key={opt.value}
              checked={selectedSet.has(opt.value)}
              onCheckedChange={() => toggle(opt.value)}
              onSelect={(e) => e.preventDefault()}
            >
              {opt.label}
            </DropdownMenuCheckboxItem>
          ))}
          {value.length > 0 && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={(e) => {
                  e.preventDefault();
                  clear();
                }}
                className="justify-center text-muted-foreground"
              >
                Xóa lọc
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
```

### 2. Lưu ý implementation
- `onSelect={(e) => e.preventDefault()}` trên từng `DropdownMenuCheckboxItem` để menu **không đóng** khi tick (user có thể chọn nhiều mục liên tục).
- Trigger button có `aria-label` để accessibility (đặc biệt cho test).
- Width content: dùng CSS var `--radix-dropdown-menu-trigger-width` để menu rộng bằng trigger (chuẩn shadcn-ui pattern).
- Generic `<T extends string>` đảm bảo type-safe với enum union (vd: `QuotationStatus`).
- Không export default — dùng named export theo convention ui/ folder (xem `dropdown-menu.tsx`).

## Verification

```powershell
cd frontend ; npm run typecheck
```

Không có test riêng cho component này ở phase này — verify khi tích hợp ở Phase 03.

## Exit Criteria
- File `frontend/src/components/ui/multi-select.tsx` tồn tại với named export `MultiSelect`.
- `npm run typecheck` pass.
- Không thêm dep mới vào `frontend/package.json` (kiểm tra git diff).
