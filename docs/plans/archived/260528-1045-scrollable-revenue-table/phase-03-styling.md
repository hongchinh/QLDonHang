# Phase 03 — Styling & Visual Integration

**Status:** [ ] pending | [ ] in-progress | [ ] complete
**Complexity:** S

## Objective
Polish styling to make the table header sticky during scroll and ensure the totals section is visually distinct and properly aligned with the table.

## Files
- `frontend/src/pages/reports/sales-revenue-detail-page.tsx`

## Tasks

### Task 1: Make table header sticky

1. **Write the failing test** (visual regression test)
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('Sticky table header', () => {
     it('applies sticky positioning to TableHeader', () => {
       const items: SalesRevenueLineItemDto[] = Array(50)
         .fill(null)
         .map((_, i) => createMockItem({ productName: `Product ${i}` }));

       const { container } = render(
         <SalesRevenueDetailPage />
       );

       const tableHeader = container.querySelector('thead');
       expect(tableHeader).toHaveClass('sticky');
       expect(tableHeader).toHaveClass('top-0');
     });
   });
   ```
   Expected: FAIL

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   In the JSX, add Tailwind classes to the `<TableHeader>` element:
   ```typescript
   <TableHeader className="sticky top-0 bg-background">
   ```
   This makes the header sticky at the top of the scrollable container and prevents content from showing through.

4. Run tests to verify they pass
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```
   Expected: PASS

5. Commit
   ```bash
   git add frontend/src/pages/reports/sales-revenue-detail-page.tsx \
           frontend/src/pages/reports/sales-revenue-detail-page.test.tsx
   git commit -m "feat: make table header sticky during scroll

   Adds sticky positioning to TableHeader so column labels remain visible
   when scrolling through large datasets. Background color prevents
   content bleed-through.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

### Task 2: Style totals section for visual distinction

1. **Write the failing test**
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('TotalsRow styling', () => {
     it('renders totals with muted background and bold text', () => {
       const { container } = render(
         <TotalsRow
           totals={{
             quantity: 100,
             lineTotal: 50000,
             freight: 5000,
             unitCost: 20000,
             lineCost: 20000,
             lineProfit: 30000,
           }}
           hasCost={true}
         />
       );

       const totalsRow = container.querySelector('tr');
       expect(totalsRow).toHaveClass('bg-muted');
       expect(totalsRow).toHaveClass('font-semibold');
     });
   });
   ```
   Expected: FAIL (if classes not already present from Phase 02)

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   Ensure `TotalsRow` component has these Tailwind classes on the `<tr>`:
   ```typescript
   <tr className="bg-muted font-semibold text-sm">
   ```
   Also ensure `<td>` elements have consistent padding:
   ```typescript
   <td className="px-3 py-2 text-right tabular-nums">
   ```

4. Run tests to verify they pass
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```
   Expected: PASS

5. Commit
   ```bash
   git add frontend/src/pages/reports/sales-revenue-detail-page.tsx \
           frontend/src/pages/reports/sales-revenue-detail-page.test.tsx
   git commit -m "feat: style totals section with visual distinction

   Totals row uses muted background (bg-muted) and bold font (font-semibold)
   to stand out from data rows. Consistent padding and text alignment with
   table cells.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

### Task 3: Ensure column alignment between table and totals

1. **Write the failing test** (integration test)
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('Table and totals column alignment', () => {
     it('renders same number of columns in table and totals', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ unitCost: 50, lineCost: 50, lineProfit: 50 }),
       ];

       const { container } = render(
         <div>
           {/* Simulate table rendering */}
           <table>
             <thead>
               <tr>
                 <th>Col1</th>
                 <th>Col2</th>
                 <th>Col3</th>
                 <th>Col4</th>
                 <th>Col5</th>
                 <th>Col6</th>
                 <th>Col7</th>
                 <th>Col8</th>
                 <th>Col9</th>
                 <th>Col10</th>
                 <th>Col11</th>
                 <th>Col12</th>
                 <th>Col13</th>
                 <th>Col14</th>
               </tr>
             </thead>
           </table>
           <TotalsRow
             totals={calculateRevenueTotals(items)}
             hasCost={true}
           />
         </div>
       );

       const headerCols = container.querySelectorAll('thead th').length;
       const totalsCols = container.querySelectorAll('tr td, tr th').length;
       expect(headerCols).toBe(totalsCols);
     });
   });
   ```
   Expected: FAIL initially until all columns are properly mapped

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   Count the columns in the actual table header (from original code):
   - Số BG, Ngày BG, Ngày XN, Khách hàng, Hàng hóa, Quy cách, ĐVT, SL, Đơn giá, Số tiền, Vận chuyển, [ĐG nhập, TT nhập, Lợi nhuận if hasCost], Địa chỉ, Điện thoại = 15 columns (or 18 with cost)

   Verify `TotalsRow` has matching column count with `colSpan` attributes where needed:
   ```typescript
   <td colSpan={3} /> // Số BG, Ngày BG, Ngày XN
   <td /> // Khách hàng
   <td /> // Hàng hóa
   <td /> // Quy cách
   <td /> // ĐVT
   <td className="...">SL total</td>
   <td /> // Đơn giá (no total)
   <td className="...">Số tiền total</td>
   <td className="...">Vận chuyển total</td>
   {hasCost && (
     <>
       <td className="...">ĐG nhập total</td>
       <td className="...">TT nhập total</td>
       <td className="...">Lợi nhuận total</td>
     </>
   )}
   <td colSpan={2} /> // Địa chỉ, Điện thoại
   ```

4. Run tests to verify they pass
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```
   Expected: PASS

5. Commit
   ```bash
   git add frontend/src/pages/reports/sales-revenue-detail-page.tsx \
           frontend/src/pages/reports/sales-revenue-detail-page.test.tsx
   git commit -m "fix: align totals row columns with table columns

   Uses colSpan to match table structure. TotalsRow now has same column
   count as TableHeader, ensuring proper visual alignment when scrolling.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

### Task 4: Manual visual verification in browser

1. **Start dev server and navigate to page**
   ```bash
   cd frontend && npm run dev
   ```
   Navigate to a sales revenue detail page (e.g., `/reports/sales-revenue-detail?from=2026-01-01&to=2026-05-31&saleUserId=<user-id>`)

2. **Verify visual correctness**
   Checklist:
   - [ ] Table has fixed height (does not expand with many rows)
   - [ ] Table scrolls vertically when content exceeds container
   - [ ] Table header stays visible at top while scrolling
   - [ ] Table header has light background and doesn't show content underneath
   - [ ] Horizontal scroll still works for wide tables
   - [ ] Totals section appears below table
   - [ ] Totals row has distinct muted background
   - [ ] Totals values are right-aligned and match column positions
   - [ ] Money formatting is consistent (uses . for thousands)
   - [ ] Quantity total is correct
   - [ ] Line total sum is correct
   - [ ] Cost columns only show when data present
   - [ ] No layout breaks on different screen widths

3. **Fix any visual issues**
   If spacing or alignment is off, adjust `px-3 py-2` padding or `colSpan` values in `TotalsRow`.

4. **Run full test suite**
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```
   Expected: All tests pass

5. **No commit needed** — visual verification is verification only, not code changes (unless step 3 required fixes)

## Verification
- All tests pass: `npm run test -- sales-revenue-detail-page.test.tsx`
- No TypeScript errors: `npm run type-check`
- App builds without errors: `npm run build`
- Visual inspection in browser matches requirements

## Exit Criteria
- ✅ Table header is sticky during scroll
- ✅ Totals row has distinct styling (muted background, bold text)
- ✅ Column alignment between table and totals is correct
- ✅ All 3 new tests pass
- ✅ Manual visual verification complete and approved
- ✅ No regressions in other report pages
