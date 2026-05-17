# Phase 03 — Global Search (BE + FE)

**Status:** [ ] pending
**Complexity:** M

## Objective
Replace `<HeaderSearchPlaceholder>` (từ phase 01) bằng search component thật: query KH + báo giá theo keyword, popover hiện 5 mỗi nhóm, click navigate. Hỗ trợ `Ctrl/Cmd+K`. Permission gating: nhóm KH ẩn nếu thiếu `customers.view`; nhóm báo giá scope theo `quotations.view_all` (admin/manager) vs `quotations.view` (chỉ của user).

## Files

**Tạo mới**
- `backend/src/OrderMgmt.Application/Search/Interfaces/ISearchService.cs`
- `backend/src/OrderMgmt.Application/Search/Services/SearchService.cs`
- `backend/src/OrderMgmt.Application/Search/Models/GlobalSearchResultDto.cs`
- `backend/src/OrderMgmt.Application/Search/Models/QuotationSearchItemDto.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/SearchController.cs`
- `frontend/src/features/search/api.ts`
- `frontend/src/features/search/hooks.ts`
- `frontend/src/components/layout/header/header-search.tsx` (desktop popover)
- `frontend/src/components/layout/header/header-search-mobile-sheet.tsx` (mobile fullscreen overlay)
- `frontend/src/lib/use-debounced-value.ts`
- `frontend/src/components/ui/popover.tsx` (shadcn wrapper cho Radix Popover)
- `frontend/src/components/ui/sheet.tsx` (shadcn — nếu chưa có, cho mobile search overlay)

**Sửa**
- `backend/src/OrderMgmt.WebApi/Program.cs` — DI `ISearchService`
- `frontend/package.json` — thêm `@radix-ui/react-popover` + `@radix-ui/react-dialog` (cho sheet)
- `frontend/src/components/layout/header/app-header.tsx` — thay placeholder bằng `<HeaderSearch />`; thay `HeaderSearchMobileButton` bằng `<HeaderSearchMobileSheet />` (trigger là button đã có ở phase 01)

**Xoá**
- `frontend/src/components/layout/header/header-search-placeholder.tsx`
- `frontend/src/components/layout/header/header-search-mobile-button.tsx` (replaced by sheet trigger)

## Tasks

1. Cài Radix Popover:
   ```
   cd frontend && npm i @radix-ui/react-popover
   ```

2. Tạo `frontend/src/components/ui/popover.tsx` wrapper (Root, Trigger, Content, Anchor):
   - Pattern từ shadcn docs (https://ui.shadcn.com/docs/components/popover).
   - Content style: `bg-popover text-popover-foreground border rounded-md shadow-md p-2 z-50` — match style các shadcn component khác trong repo (xem `dropdown-menu.tsx` cho reference class).

3. Tạo DTO trong `OrderMgmt.Application/Search/Models/`:
   ```csharp
   public sealed class GlobalSearchResultDto {
       public List<CustomerSearchItemDto> Customers { get; set; } = new();
       public List<QuotationSearchItemDto> Quotations { get; set; } = new();
   }
   public sealed record QuotationSearchItemDto(
       Guid Id, string Code, string CustomerName, decimal Total, string Status, DateTimeOffset CreatedAt);
   ```
   - `CustomerSearchItemDto` reuse từ `OrderMgmt.Application/Catalog/Customers/Models/` (đã có — xem `CustomersController.Search`).

4. Tạo `ISearchService.GlobalAsync(string keyword, CancellationToken ct) → GlobalSearchResultDto`:
   - DI: `AppDbContext`, `ICurrentUser` (hoặc pattern hiện tại để biết current user + permissions).

5. Implement `SearchService`:
   - **Min length 3** (bumped từ 2 — 2 ký tự match quá nhiều, vd "an" match cả trăm KH): nếu `keyword.Length < 3` → return empty DTO.
   - **Customers** branch: chỉ query nếu `currentUser.HasPermission("customers.view")`.
     ```csharp
     var customersTask = currentUser.HasPermission("customers.view")
         ? ctx.Customers.AsNoTracking()
             .Where(c => !c.IsDeleted
                 && (EF.Functions.ILike(c.Name, $"%{kw}%")
                     || EF.Functions.ILike(c.Phone, $"%{kw}%")))
             .OrderBy(c => c.Name)
             .Take(5)
             .Select(c => new CustomerSearchItemDto(...))
             .ToListAsync(ct)
         : Task.FromResult(new List<CustomerSearchItemDto>());
     ```
   - **Quotations** branch: chỉ query nếu user có ít nhất `quotations.view`.
     ```csharp
     IQueryable<Quotation> qQuery = ctx.Quotations.AsNoTracking()
         .Where(q => !q.IsDeleted
             && (EF.Functions.ILike(q.Code, $"%{kw}%")
                 || EF.Functions.ILike(q.CustomerName, $"%{kw}%")));
     if (!currentUser.HasPermission("quotations.view_all"))
         qQuery = qQuery.Where(q => q.OwnerId == currentUser.UserId);
     var quotationsTask = currentUser.HasPermission("quotations.view")
         ? qQuery.OrderByDescending(q => q.CreatedAt).Take(5)
             .Select(q => new QuotationSearchItemDto(...))
             .ToListAsync(ct)
         : Task.FromResult(new List<QuotationSearchItemDto>());
     ```
   - **Parallel execution** (giảm latency ~50% vì 2 query độc lập):
     ```csharp
     await Task.WhenAll(customersTask, quotationsTask);
     return new GlobalSearchResultDto {
         Customers = await customersTask,
         Quotations = await quotationsTask
     };
     ```
     ⚠️ Nếu `AppDbContext` là scoped (single instance) → 2 query song song trên cùng context sẽ throw. Trong trường hợp đó: dùng `IDbContextFactory<AppDbContext>` tạo 2 context riêng cho 2 task. Verify pattern hiện có trong repo trước khi implement; nếu chưa có factory → đăng ký trong `Program.cs`: `services.AddDbContextFactory<AppDbContext>()`.

6. Tạo `SearchController`:
   ```csharp
   [Authorize]
   public class SearchController : ApiControllerBase {
       private readonly ISearchService _search;
       public SearchController(ISearchService search) { _search = search; }

       [HttpGet("/api/search/global")]
       public async Task<ActionResult<ApiResponse<GlobalSearchResultDto>>> Global(
           [FromQuery] string q, CancellationToken ct)
           => Success(await _search.GlobalAsync(q ?? string.Empty, ct));
   }
   ```

7. DI trong `Program.cs`:
   ```csharp
   services.AddScoped<ISearchService, SearchService>();
   ```

8. Tạo `frontend/src/lib/use-debounced-value.ts`:
   ```ts
   import { useEffect, useState } from 'react';
   export function useDebouncedValue<T>(value: T, delay: number): T {
       const [debounced, setDebounced] = useState(value);
       useEffect(() => {
           const id = setTimeout(() => setDebounced(value), delay);
           return () => clearTimeout(id);
       }, [value, delay]);
       return debounced;
   }
   ```

9. Tạo `frontend/src/features/search/api.ts`:
   - `globalSearch(q: string): Promise<GlobalSearchResult>` — GET `/api/search/global?q=...`.

10. Tạo `frontend/src/features/search/hooks.ts`:
    - `useGlobalSearch(query: string)`: React Query, key `['search', query]`, `enabled: query.length >= 3`, staleTime `0`, refetchOnWindowFocus `false`.
    - Pattern: caller dùng `useDebouncedValue` trước khi pass vào hook.

11. Tạo `frontend/src/components/layout/header/header-search.tsx`:
    - State: `inputValue` (controlled), `open` (popover).
    - Debounced query: `const debounced = useDebouncedValue(inputValue, 250);`
    - `const { data, isLoading } = useGlobalSearch(debounced);`
    - Layout: input nền trắng, icon Search lucide, max-w `320px` (responsive: `240px` ở 768-1279, `hidden` dưới 768).
    - Popover open khi `open && inputValue.length >= 3` (min length sync với BE).
    - Popover content:
      - Group "Khách hàng" (chỉ render nếu `data.customers.length > 0`): list item KH (avatar/icon + name + phone), click → `navigate('/customers/'+id)` + close popover + clear input.
      - Group "Báo giá" (tương tự): item hiển thị code + customerName + total formatted + status badge.
      - Empty state: "Không có kết quả" nếu cả 2 list rỗng và `!isLoading`.
      - Loading skeleton khi `isLoading`.
    - Keyboard:
      - Arrow Down/Up: navigate items (track active index).
      - Enter: select active item.
      - Escape: close popover + blur.
    - Global shortcut Ctrl/Cmd+K: `useEffect` listen `window.addEventListener('keydown', ...)` → focus input + `preventDefault()`.
    - A11y: input `aria-label="Tìm kiếm toàn cục"`; popover content `role="listbox"`; items `role="option"` với `aria-selected`.

12. Cài shadcn sheet (nếu chưa có): `npx shadcn@latest add sheet`. Tạo `frontend/src/components/layout/header/header-search-mobile-sheet.tsx`:
    - Trigger: icon button Search (visible `md:hidden`), `aria-label="Mở tìm kiếm"`.
    - Open → `Sheet` từ Radix Dialog: fullscreen overlay (`side="top"` hoặc full overlay), input lớn ở top + same search logic as desktop (reuse `useGlobalSearch` + group rendering).
    - Esc hoặc tap outside → close.
    - Click result → navigate + close sheet.
    - **Lý do**: review note #6 — không được mất feature search trên mobile.

13. Sửa `app-header.tsx`:
    - Import `HeaderSearch` thay `HeaderSearchPlaceholder`.
    - Render `<HeaderSearch />` (desktop) + `<HeaderSearchMobileSheet />` (mobile).
    - Xoá usage `HeaderSearchMobileButton` (đã được sheet thay thế).

14. Xoá file `frontend/src/components/layout/header/header-search-placeholder.tsx` + `header-search-mobile-button.tsx`.

15. Build BE:
    ```
    dotnet build backend/src/OrderMgmt.Application backend/src/OrderMgmt.WebApi
    ```

16. FE checks:
    ```
    cd frontend && npm run typecheck && npm run lint && npm run test
    ```

17. Manual test:
    - Login admin → gõ tên KH → popover hiện sau ~250ms → 5 KH max → click → navigate `/customers/:id`.
    - Gõ mã báo giá (vd "BG-001") → popover hiện báo giá → click → navigate `/quotations/:id`.
    - Login user Sales (chỉ có `quotations.view`, không có `customers.view`) → gõ keyword → chỉ hiện group "Báo giá", và chỉ báo giá của mình.
    - Gõ < 3 ký tự → không hiện popover, không có Network request.
    - Ctrl+K từ bất kỳ trang → focus search input.
    - Escape → close popover.
    - Arrow keys + Enter → navigate + select.
    - **Mobile <768px**: tap icon Search → fullscreen sheet mở; gõ keyword → kết quả hiện; tap result → navigate + close sheet.
    - **Parallel verify**: log timing 2 query trong BE; tổng latency ≈ max(customerQuery, quotationQuery), không phải sum.

## Verification

```
# Backend
dotnet build backend/src/OrderMgmt.Application backend/src/OrderMgmt.WebApi

# Frontend
cd frontend && npm run typecheck && npm run lint && npm run test
```

## Exit Criteria
- [ ] `GET /api/search/global?q=abc` (≥3 ký tự) trả `{customers: [...≤5], quotations: [...≤5]}`.
- [ ] `q` <3 ký tự → cả 2 list rỗng (200 OK).
- [ ] 2 query KH + báo giá chạy parallel (xác minh qua log timing — tổng ≈ max chứ không phải sum).
- [ ] User không có `customers.view` → `customers: []`.
- [ ] User không có `quotations.view_all` → chỉ thấy báo giá của mình (verify bằng test với 2 user khác nhau).
- [ ] FE: debounce 250ms hoạt động (Network tab không có request mỗi keystroke).
- [ ] FE: gõ < 3 ký tự → không gọi API.
- [ ] Click result → navigate đúng + popover close + input clear.
- [ ] Ctrl/Cmd+K focus search từ bất kỳ trang.
- [ ] Esc close popover.
- [ ] Arrow keys navigate items; Enter select active.
- [ ] Mobile <768px: search sheet mở/đóng/tìm/navigate hoạt động đầy đủ (không "hidden" như phase 01).
