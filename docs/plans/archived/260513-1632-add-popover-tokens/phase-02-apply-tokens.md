# Phase 02 — Apply popover tokens + verification

**Status:** [x] complete
**Complexity:** S

## Objective
Thêm tokens `--popover` / `--popover-foreground` vào [index.css](../../../frontend/src/index.css) (light + dark) và đăng ký color `popover` trong [tailwind.config.ts](../../../frontend/tailwind.config.ts), sao cho Tailwind emit utility `bg-popover` / `text-popover-foreground` đúng giá trị HSL. Không sửa JSX.

## Files
- `frontend/src/index.css` (modify — thêm 2 dòng `:root`, 2 dòng `.dark`)
- `frontend/tailwind.config.ts` (modify — thêm 4 dòng `popover: { ... }` trong `theme.extend.colors`)

## Tasks

### 1. Edit `frontend/src/index.css`
Trong block `:root` (sau dòng `--card-foreground: 222.2 84% 4.9%;`), thêm:
```css
--popover: 0 0% 100%;
--popover-foreground: 222.2 84% 4.9%;
```

Trong block `.dark` (sau dòng `--card-foreground: 210 40% 98%;`), thêm:
```css
--popover: 222.2 84% 4.9%;
--popover-foreground: 210 40% 98%;
```

### 2. Edit `frontend/tailwind.config.ts`
Trong object `theme.extend.colors`, sau entry `card`, thêm:
```ts
popover: {
  DEFAULT: 'hsl(var(--popover))',
  foreground: 'hsl(var(--popover-foreground))',
},
```

### 3. Static verification (chạy ở `frontend/`)
```
npm run lint
npm run test
npm run build
```
Cả 3 phải pass. Không có warning Tailwind mới (Tailwind sẽ tự pick `bg-popover` class đang ở source code và emit utility).

### 4. Manual UI verification (chạy `npm run dev` ở `frontend/`)
- Mở `http://localhost:5173/quotations/new`.
- **Case A — Customer dropdown**:
  - Focus ô tìm Khách hàng → gõ 1 ký tự bất kỳ (ví dụ "a").
  - Dropdown xổ xuống → background **TRẮNG ĐẶC**, không thấy form/element phía dưới xuyên qua.
  - Hover/Arrow xuống 1 row → row highlight `bg-accent` (xám nhạt) visible rõ ràng trên nền trắng.
- **Case B — Product typeahead dropdown** (line items grid):
  - Trong bảng "Chi tiết hàng hóa", focus 1 cell "Mã hàng" → gõ 1 ký tự.
  - Dropdown product xổ ra qua portal → background cũng **TRẮNG ĐẶC**.
  - Scroll ngang bảng → dropdown không bị xuyên thấu rows behind.
- **Case C — Dark mode (nếu app có toggle)**:
  - Toggle dark mode → cả 2 dropdown chuyển sang nền dark navy `hsl(222.2 84% 4.9%)`, text sáng.
- **Case D — Console**: F12 → không có warning/error mới liên quan CSS variable.

## Verification
- `npm run lint`: 0 error, 0 warning.
- `npm run test`: tất cả test pass (kỳ vọng 54/54 như baseline).
- `npm run build`: tsc + vite build success.
- Case A + B pass visual: dropdown nền trắng đặc.
- (Optional) Case C nếu app expose dark toggle.

## Exit Criteria
- 2 file đã edit đúng theo task 1/2.
- Static checks clean.
- Visual case A + B confirmed bằng mắt thường.
- Không regression layout/keyboard của dropdown.
- Phase status → `[x] complete`.
