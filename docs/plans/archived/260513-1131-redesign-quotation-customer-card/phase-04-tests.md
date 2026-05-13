# Phase 04 — Cập nhật tests

**Status:** [x] complete
**Complexity:** S

## Objective

Bổ sung test cho 2 hành vi mới trong `CustomerAutocomplete`: (1) input hiển thị `code` khi `value` được set; (2) popover có meta header "Tìm thấy N khách hàng đang hoạt động" và 3 phím tắt (Tab/Enter/Esc). Không cần đổi test cũ.

## Files

- `frontend/src/components/customer-autocomplete/customer-autocomplete.test.tsx`

## Tasks

1. Thêm test case **render `code` khi đã có selection** ngay sau test cuối:
   ```tsx
   it('renders code (not name) when value is selected', () => {
     renderWithClient(
       <CustomerAutocomplete
         {...baseProps({
           value: { id: sample[0].id, code: sample[0].code, name: sample[0].name },
         })}
         inputAriaLabel="cust"
       />,
     );
     const input = screen.getByRole('combobox', { name: /cust/i }) as HTMLInputElement;
     expect(input.value).toBe('KH-001');
     expect(input.value).not.toBe('Công ty TNHH ABC');
   });
   ```
2. Thêm test case **meta header hiển thị số kết quả + keyboard hints**:
   ```tsx
   it('renders meta header with result count and keyboard hints', async () => {
     renderWithClient(<CustomerAutocomplete {...baseProps()} inputAriaLabel="cust" />);
     const input = screen.getByRole('combobox', { name: /cust/i });
     await typeAndWaitForResults(input, 'cong');
     expect(screen.getByText(/Tìm thấy 3 khách hàng đang hoạt động/i)).toBeInTheDocument();
     expect(screen.getByText('Tab')).toBeInTheDocument();
     expect(screen.getByText('Enter')).toBeInTheDocument();
     expect(screen.getByText('Esc')).toBeInTheDocument();
   });
   ```
3. Thêm test case **meta header hiển thị trạng thái loading/error** (tùy chọn — nâng coverage):
   ```tsx
   it('meta header shows loading state then result count', async () => {
     renderWithClient(<CustomerAutocomplete {...baseProps()} inputAriaLabel="cust" />);
     const input = screen.getByRole('combobox', { name: /cust/i });
     fireEvent.change(input, { target: { value: 'cong' } });
     // Trong khi debounce/search đang chạy, có thể thấy 'Đang tìm kiếm...'
     await waitFor(() => {
       expect(screen.getByText(/Tìm thấy/i)).toBeInTheDocument();
     });
   });
   ```
4. Verify các test cũ không phụ thuộc vào `value.name` rendering trong input (đã xác nhận khi đọc file gốc — không có).
5. Chạy lại toàn bộ test suite cho file để đảm bảo cả test cũ và mới đều pass.

## Verification

```bash
cd frontend && pnpm test customer-autocomplete
# Tất cả test pass, bao gồm 2-3 test mới thêm vào.
```

## Exit Criteria

- File test có thêm ít nhất 2 test mới: render code, meta header.
- Tất cả test trong `customer-autocomplete.test.tsx` pass.
- Không có flaky/skip test mới.
