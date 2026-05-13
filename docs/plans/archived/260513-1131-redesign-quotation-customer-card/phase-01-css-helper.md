# Phase 01 — CSS helper + main.tsx import

**Status:** [x] complete
**Complexity:** S

## Objective

Tạo file CSS helper `form-inline.css` chứa các class cho layout inline-grid (label phải, input trải đều), import vào `main.tsx` để dùng được toàn cục. Đây là bước nền tảng cho phase 03.

## Files

- `frontend/src/styles/form-inline.css` (mới)
- `frontend/src/main.tsx`

## Tasks

1. Tạo thư mục `frontend/src/styles/` (nếu chưa có).
2. Tạo file `frontend/src/styles/form-inline.css` với nội dung:
   ```css
   .form-inline-grid {
     display: grid;
     gap: 8px 10px;
     align-items: center;
   }

   .form-inline-grid .field-label {
     text-align: right;
     white-space: nowrap;
     font-size: 0.875rem;
     line-height: 1.25rem;
     color: hsl(var(--muted-foreground));
   }

   .form-inline-grid .field-label.required::after {
     content: " *";
     color: hsl(var(--destructive));
     font-weight: 700;
   }

   .form-inline-grid .field-message {
     grid-column: 2 / -1;
     font-size: 0.75rem;
     line-height: 1rem;
     margin-top: -2px;
   }

   .form-inline-grid .field-message-code {
     grid-column: 2 / 3;
   }

   .form-inline-grid .field-span-rest {
     grid-column: span 3;
   }

   @media (max-width: 1023px) {
     .form-inline-grid.customer-row {
       grid-template-columns: 90px 1fr !important;
     }

     .form-inline-grid .field-span-rest,
     .form-inline-grid .field-message-code {
       grid-column: 2 / -1;
     }
   }
   ```
3. Mở `frontend/src/main.tsx`, thêm import `./styles/form-inline.css` ngay sau dòng `import './index.css';`:
   ```ts
   import './index.css';
   import './styles/form-inline.css';
   ```

## Verification

```bash
cd frontend && pnpm lint
cd frontend && pnpm typecheck
cd frontend && pnpm dev
# Mở DevTools, kiểm tra: document.styleSheets có chứa rule .form-inline-grid
```

## Exit Criteria

- File `frontend/src/styles/form-inline.css` tồn tại với các selector chính: `.form-inline-grid`, `.field-label`, `.field-label.required::after`, `.field-message`, `.field-message-code`, `.field-span-rest` và 1 media query cho `.customer-row`.
- `main.tsx` import đúng thứ tự (`index.css` trước, `form-inline.css` sau).
- `pnpm lint` và `pnpm typecheck` pass.
- Vite dev server không cảnh báo về CSS không tìm thấy.
