# Phase 02 — CustomerAutocomplete: hiển thị code + popover

**Status:** [x] complete (typecheck verified jointly with Phase 03)
**Complexity:** M

## Objective

Cập nhật `CustomerAutocomplete` để: (1) khi đã chọn KH, input hiển thị `code` thay vì `name`; (2) popover dropdown mở rộng tối thiểu 760px và có meta header (số kết quả + keyboard hints) phía trên bảng kết quả.

## Files

- `frontend/src/components/customer-autocomplete/customer-autocomplete.tsx`

## Tasks

1. Mở rộng interface prop `value` trong `CustomerAutocompleteProps`:
   - Hiện tại: `value: { id: string; name: string } | null;` (dòng 12).
   - Đổi thành: `value: { id: string; code: string; name: string } | null;`
2. Trong nhánh `hasSelection` (dòng 126-162), đổi `value={value.name}` (dòng 131) → `value={value.code}`. Giữ nguyên `placeholder`, `onChange` (vẫn `onClear()` rồi `setKeyword`).
3. Cập nhật popover dropdown wrapper (dòng 204):
   - Hiện tại: `<div className="absolute left-0 right-0 z-50 mt-1 max-h-80 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md">`.
   - Đổi thành: `<div className="absolute left-0 z-50 mt-1 min-w-[min(760px,calc(100vw-80px))] max-w-[calc(100vw-40px)] max-h-80 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md">`.
   - Bỏ `right-0` để không bị ép full-width input. Thêm `min-w-[...]` và `max-w-[...]`.
4. Thêm meta header ngay bên trong wrapper, **trước** `<table>` (dòng 206):
   ```tsx
   <div className="flex items-center justify-between border-b bg-muted/30 px-2 py-1.5 text-xs text-muted-foreground">
     <span>
       {isLoading
         ? 'Đang tìm kiếm...'
         : isError
         ? 'Lỗi tải danh sách'
         : `Tìm thấy ${results.length} khách hàng đang hoạt động`}
     </span>
     <span className="flex items-center gap-1.5">
       <kbd className="rounded border bg-background px-1 py-0.5 font-mono text-[10px]">Tab</kbd>
       duyệt
       <kbd className="rounded border bg-background px-1 py-0.5 font-mono text-[10px]">Enter</kbd>
       chọn
       <kbd className="rounded border bg-background px-1 py-0.5 font-mono text-[10px]">Esc</kbd>
       đóng
     </span>
   </div>
   ```
5. Verify import `kbd` không cần thiết (kbd là HTML native element).
6. Kiểm tra `handleClear` (dòng 73-78) và các hàm khác vẫn hoạt động — không phụ thuộc vào `value.name` cụ thể.

## Verification

```bash
cd frontend && pnpm lint
cd frontend && pnpm typecheck
cd frontend && pnpm test customer-autocomplete
# Test cũ phải pass (không assert giá trị input sau khi chọn).
```

Manual:
- Mở Storybook (nếu có) hoặc `/quotations/new`.
- Gõ → popover bung, hiển thị "Tìm thấy N khách hàng đang hoạt động" và 3 kbd.
- Chọn → input hiển thị `KH-001` (code), không phải `Công ty ABC` (name).
- Resize cửa sổ nhỏ → popover không tràn viewport (`max-w` giới hạn).

## Exit Criteria

- Interface `CustomerAutocompleteProps.value` chứa `code: string`.
- Input hiển thị `code` khi đã chọn (verify bằng manual hoặc test mới ở phase 04).
- Popover có meta header với 3 kbd + label đúng.
- Popover width: tối thiểu 760px hoặc full viewport-padding, anchor trái.
- `pnpm lint` và `pnpm typecheck` pass.
- `pnpm test customer-autocomplete` các test cũ vẫn pass.
