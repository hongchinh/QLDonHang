# Phase 02 — Layout & Structure Changes

**Status:** [ ] pending | [ ] in-progress | [ ] complete
**Complexity:** M

## Objective
Restructure the table layout to support fixed height with internal scrolling and add the totals section component.

## Files
- `frontend/src/pages/reports/sales-revenue-detail-page.tsx`
- `frontend/src/pages/reports/sales-revenue-detail-page.test.tsx`

## Tasks

### Task 1: Add test for useRevenueTotals hook

1. **Write the failing test**
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('useRevenueTotals hook', () => {
     it('returns totals from items using useMemo', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ quantity: 5, lineTotal: 500 }),
         createMockItem({ quantity: 3, lineTotal: 300 }),
       ];
       const { result } = renderHook(() => useRevenueTotals(items));
       expect(result.current.quantity).toBe(8);
       expect(result.current.lineTotal).toBe(800);
     });
   });
   ```
   Expected: FAIL with `Cannot find name 'useRevenueTotals'`

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   Add hook to `sales-revenue-detail-page.tsx`:
   ```typescript
   function useRevenueTotals(items: SalesRevenueLineItemDto[]): RevenueTotals {
     return useMemo(() => calculateRevenueTotals(items), [items]);
   }
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
   git commit -m "feat: add useRevenueTotals hook with memoization

   Hook wraps calculateRevenueTotals with useMemo to prevent recalculation
   on every render. Tested to ensure totals are computed correctly.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

### Task 2: Create TotalsRow component

1. **Write the failing test**
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('TotalsRow component', () => {
     it('renders totals with correct money formatting', () => {
       render(
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
       expect(screen.getByText(/100/)).toBeInTheDocument(); // quantity
       expect(screen.getByText(/50\.000/)).toBeInTheDocument(); // lineTotal in VND format
     });

     it('hides cost columns when hasCost is false', () => {
       const { container } = render(
         <TotalsRow
           totals={{
             quantity: 10,
             lineTotal: 5000,
             freight: 500,
             unitCost: 0,
             lineCost: 0,
             lineProfit: 0,
           }}
           hasCost={false}
         />
       );
       const rows = container.querySelectorAll('tr');
       expect(rows.length).toBeGreaterThan(0);
     });
   });
   ```
   Expected: FAIL with `TotalsRow is not defined`

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   Add component to `sales-revenue-detail-page.tsx`:
   ```typescript
   interface TotalsRowProps {
     totals: RevenueTotals;
     hasCost: boolean;
   }

   function TotalsRow({ totals, hasCost }: TotalsRowProps) {
     return (
       <div className="overflow-x-auto border-t">
         <table className="w-full">
           <tbody>
             <tr className="bg-muted font-semibold">
               <td className="px-3 py-2 text-sm">Tổng cộng</td>
               <td colSpan={3} />
               <td />
               <td />
               <td />
               <td className="px-3 py-2 text-right tabular-nums">
                 {moneyFmt.format(totals.quantity)}
               </td>
               <td />
               <td className="px-3 py-2 text-right tabular-nums">
                 {moneyFmt.format(totals.lineTotal)}
               </td>
               <td className="px-3 py-2 text-right tabular-nums">
                 {moneyFmt.format(totals.freight)}
               </td>
               {hasCost && (
                 <>
                   <td className="px-3 py-2 text-right tabular-nums">
                     {moneyFmt.format(totals.unitCost)}
                   </td>
                   <td className="px-3 py-2 text-right tabular-nums">
                     {moneyFmt.format(totals.lineCost)}
                   </td>
                   <td className="px-3 py-2 text-right tabular-nums">
                     {moneyFmt.format(totals.lineProfit)}
                   </td>
                 </>
               )}
               <td colSpan={2} />
             </tr>
           </tbody>
         </table>
       </div>
     );
   }
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
   git commit -m "feat: add TotalsRow component for revenue summary

   Displays formatted totals for quantity, amounts, freight, and profit.
   Conditionally renders cost columns based on data availability.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

### Task 3: Wrap table in scrollable container with fixed height

1. **Write the failing test**
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('Scrollable table container', () => {
     it('applies max-height and overflow styles to table container', () => {
       const items: SalesRevenueLineItemDto[] = Array(50)
         .fill(null)
         .map((_, i) => createMockItem({ productName: `Product ${i}` }));
       
       const { container } = render(
         <SalesRevenueDetailPage />
       );
       
       const tableContainer = container.querySelector('[data-testid="table-scroll-container"]');
       expect(tableContainer).toHaveClass('overflow-y-auto');
       expect(tableContainer).toHaveStyle({ maxHeight: 'calc(100vh - 400px)' });
     });
   });
   ```
   Expected: FAIL with `Unable to find element with test id "table-scroll-container"`

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   In `SalesRevenueDetailPage`, wrap the `overflow-x-auto` div containing `<Table>` with:
   ```typescript
   <div 
     data-testid="table-scroll-container"
     className="overflow-y-auto"
     style={{ maxHeight: 'calc(100vh - 400px)' }}
   >
     <div className="overflow-x-auto">
       <Table>
         {/* existing table content */}
       </Table>
     </div>
   </div>
   ```
   (Replaces the current `<div className="overflow-x-auto">` wrapper)

4. Run tests to verify they pass
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```
   Expected: PASS

5. Commit
   ```bash
   git add frontend/src/pages/reports/sales-revenue-detail-page.tsx \
           frontend/src/pages/reports/sales-revenue-detail-page.test.tsx
   git commit -m "feat: add scrollable container with fixed height to table

   Wraps table in container with max-height calc(100vh - 400px) and
   overflow-y-auto to enable internal scrolling while keeping header/footer visible.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

### Task 4: Add TotalsRow below table in CardContent

1. **Write the failing test**
   Add this test to `sales-revenue-detail-page.test.tsx`:
   ```typescript
   describe('TotalsRow placement', () => {
     it('renders TotalsRow below table with correct props', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ quantity: 10, lineTotal: 1000 }),
       ];
       
       // Mock the hook to return test data
       vi.mocked(useSalesRevenueDetail).mockReturnValue({
         data: items,
         isLoading: false,
         isError: false,
       } as any);
       
       render(<SalesRevenueDetailPage />);
       
       expect(screen.getByText('Tổng cộng')).toBeInTheDocument();
     });
   });
   ```
   Expected: FAIL

2. Run test to verify failure
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   In `SalesRevenueDetailPage` component's return JSX, add after the closing `</div>` of the table container:
   ```typescript
   {items.length > 0 && (
     <TotalsRow totals={useRevenueTotals(items)} hasCost={hasCost} />
   )}
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
   git commit -m "feat: integrate TotalsRow into page below table

   Renders totals summary after the scrollable table, only when items exist.
   Passes hasCost flag to conditionally show cost columns.
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

## Verification
- All tests pass: `npm run test -- sales-revenue-detail-page.test.tsx`
- No TypeScript errors: `npm run type-check`
- Page compiles: `npm run build`

## Exit Criteria
- ✅ `useRevenueTotals` hook exists and uses memoization
- ✅ `TotalsRow` component is implemented with conditional cost columns
- ✅ Table wrapped in scrollable container with `max-height: calc(100vh - 400px)`
- ✅ TotalsRow renders below table when items exist
- ✅ All 4 new tests pass
