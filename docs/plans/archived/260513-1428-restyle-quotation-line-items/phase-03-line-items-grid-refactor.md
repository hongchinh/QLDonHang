# Phase 03 — LineItemsGrid refactor + keyboard + delete-all

**Status:** [x] complete
**Complexity:** L

## Objective
Thay thế shadcn `Table`/`Input` trong `LineItemsGrid` bằng native `<table class="accounting-grid">` + `<input class="cell-input">`. Thêm state `activeRowIndex`, wire keyboard (`Insert`, `Ctrl+Delete`), thêm nút "Xóa tất cả dòng" với `ConfirmDialog` hiện có của project. Footer hiển thị kbd hints + Tổng subtotal.

## Files
- `frontend/src/pages/quotations/components/line-items-grid.tsx`
- `frontend/src/pages/quotations/components/line-items-grid.test.tsx` (mới, targeted tests)

## Tasks

1. **Imports**:
   - Bỏ import `Badge`, `Input`, `Table`, `TableBody`, `TableCell`, `TableHead`, `TableHeader`, `TableRow`.
   - Bỏ import `Button`; dùng native `<button>` để khớp CSS `.lib-btn` / `.cell-action`.
   - Chỉ giữ import `Plus`, `Trash2` từ `lucide-react`.
   - Thêm `import { useEffect, useRef, useState } from 'react'`.
   - Thêm `import { ConfirmDialog } from '@/components/ui/confirm-dialog';`.
   - Thêm `import './line-items-grid.css'` (relative — Vite resolves).

2. **State + refs mới**:
   ```ts
   const wrapRef = useRef<HTMLDivElement>(null);
   const [activeRowIndex, setActiveRowIndex] = useState<number | null>(null);
   const [clearAllOpen, setClearAllOpen] = useState(false);
   ```

3. **Keyboard handler**:
   ```ts
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
   }, [activeRowIndex, fields.length]);
   ```
   Vì `addLine`/`remove` là closure stable từ `useFieldArray`, vẫn cần dependency `fields.length` để `Ctrl+Delete` không xóa index ngoài range. Thêm `// eslint-disable-next-line react-hooks/exhaustive-deps` nếu lint kêu missing deps cho `addLine`/`remove`.

4. **Track focus → activeRowIndex**:
   Trên `<tr>`, thêm `onFocus={() => setActiveRowIndex(idx)}` (focus events bubble từ input lên tr).

5. **Subtotal cho footer**:
   ```ts
   const subtotal = rows.reduce((sum, line) => sum + computeLineTotal(toLineLike(line)), 0);
   ```

6. **Confirm xóa tất cả bằng `ConfirmDialog`**:
   ```ts
   function clearAllLines() {
     for (let i = fields.length - 1; i >= 0; i--) remove(i);
     setActiveRowIndex(null);
     setClearAllOpen(false);
   }

   function handleClearAll() {
     if (fields.length === 0) return;
     setClearAllOpen(true);
   }
   ```

7. **JSX mới** — thay toàn bộ `return (...)`:

```tsx
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
                    value={(line.productCode ?? '') as string}
                    onChange={(v) => setLineField(idx, 'productCode', v)}
                    onSelect={(s) => {
                      update(idx, {
                        ...line,
                        productId: s.id,
                        productCode: s.code,
                        productName: s.name,
                        specification: s.specification ?? '',
                        unitName: s.unitName ?? line.unitName,
                        pricingMode: s.pricingMode,
                        unitPrice: s.defaultPrice ?? 0,
                        unitCost: s.costPrice,
                      } as QuotationLineFormValues);
                    }}
                  />
                </td>
                <td>
                  <input
                    className="cell-input"
                    value={(line.productName ?? '') as string}
                    onChange={(e) => setLineField(idx, 'productName', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    className="cell-input"
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
                    value={numInput(line.quantity)}
                    onChange={(e) => setLineField(idx, 'quantity', (parseNum(e.target.value) ?? 0) as never)}
                  />
                </td>
                <td className="cell-number">
                  <input
                    className="cell-input cell-number"
                    type="number"
                    step="any"
                    value={numInput(line.unitPrice)}
                    onChange={(e) => setLineField(idx, 'unitPrice', (parseNum(e.target.value) ?? 0) as never)}
                  />
                </td>
                <td className="cell-number">
                  <input
                    className="cell-input cell-number"
                    type="number"
                    step="any"
                    value={numInput(line.unitCost)}
                    onChange={(e) => setLineField(idx, 'unitCost', parseNum(e.target.value) as never)}
                  />
                </td>
                <td className="cell-number">
                  <div className="cell-readonly cell-number">{fmt.format(lineTotal)}</div>
                  {lineCost != null && (
                    <div className="cell-readonly cell-number" style={{ fontSize: 11, opacity: 0.7, height: 14 }}>
                      LN: {fmt.format(lineTotal - lineCost)}
                    </div>
                  )}
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
              <td colSpan={11} style={{ textAlign: 'center', color: '#6b7280', padding: '12px', background: '#fbfdff' }}>
                Chưa có dòng nào. Bấm "Thêm dòng" hoặc phím <span className="kbd" style={{ fontFamily: 'Consolas, monospace', padding: '1px 5px', border: '1px solid #b8c2cc', borderRadius: 3 }}>Insert</span>.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>

    <div className="line-items-footer">
      <div className="keyboard-guide">
        <span><span className="kbd">Tab</span> di chuyển</span>
        <span><span className="kbd">Enter</span> chọn</span>
        <span><span className="kbd">Insert</span> Thêm dòng</span>
        <span><span className="kbd">Ctrl</span>+<span className="kbd">Delete</span> Xóa dòng</span>
      </div>
      <strong>Tổng: {fmt.format(subtotal)}</strong>
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
```

8. **Bỏ JSX cũ** (block `<Table>...</Table>` và `<Button>Thêm dòng</Button>` ở dưới) sau khi đã thay xong.

9. **Type check kĩ**: `update`, `setLineField`, `addLine`, `remove`, `toLineLike` đều giữ nguyên — chỉ JSX đổi. Đảm bảo `setActiveRowIndex` không gây render loop (chỉ trigger khi `onFocus` trên `<tr>`).

10. **Targeted tests**:
   - Tạo `line-items-grid.test.tsx` nếu chưa có.
   - Mock `ProductTypeaheadCell` thành input đơn giản để test `LineItemsGrid` không phụ thuộc API search sản phẩm.
   - Test tối thiểu:
     - render dòng hiện có và subtotal footer đúng.
     - click "Thêm dòng" thêm một dòng.
     - click trash từng dòng xóa đúng dòng.
     - click "Xóa tất cả dòng" mở `ConfirmDialog`, bấm "Xóa" thì xóa hết dòng và hiện placeholder row.
     - focus input trong dòng 2 rồi fire `Ctrl+Delete` thì xóa dòng 2.

## Verification
- Chạy `npm run lint` ở `frontend/` — chỉ chấp nhận warning cũ (nếu có), không thêm lỗi/warning mới (trừ `react-hooks/exhaustive-deps` đã disable một dòng).
- Chạy `npm run build` — pass.
- Chạy `npm run test -- line-items-grid` — pass.
- Manual (sẽ làm cụ thể ở Phase 06): trang `/quotations/new` render bảng theo style mockup; gõ phím vẫn ổn; Insert/Ctrl+Delete chạy; "Xóa tất cả" có confirm.

## Exit Criteria
- File compile sạch; type check pass.
- `LineItemsGrid` không còn import từ `@/components/ui/table` hay `@/components/ui/badge`.
- Có import `./line-items-grid.css` và sử dụng class `.accounting-grid` v.v.
- Nút "Xóa tất cả dòng" tồn tại + `ConfirmDialog` được hiển thị trước khi xóa.
- Keyboard listener bind/cleanup đúng trong `useEffect`.
- Targeted tests cho add/remove/clear-all/subtotal/keyboard pass.
