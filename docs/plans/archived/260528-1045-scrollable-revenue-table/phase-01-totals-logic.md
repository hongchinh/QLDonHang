# Phase 01 — Totals Calculation Logic

**Status:** [ ] pending | [ ] in-progress | [ ] complete
**Complexity:** S

## Objective
Create and test the totals calculation logic that will aggregate quantity, amounts, freight, and profit columns across all line items.

## Files
- `frontend/src/pages/reports/sales-revenue-detail-page.tsx`
- `frontend/src/pages/reports/sales-revenue-detail-page.test.tsx` (new)

## Tasks

### Task 1: Create totals calculation utility function

1. **Write the failing test**
   ```bash
   cat > frontend/src/pages/reports/sales-revenue-detail-page.test.tsx << 'EOF'
   import { describe, expect, it } from 'vitest';
   import type { SalesRevenueLineItemDto } from '@/features/reports/sales-revenue-detail/types';
   import { calculateRevenueTotals } from './sales-revenue-detail-page';
   
   describe('calculateRevenueTotals', () => {
     it('returns zero totals for empty items array', () => {
       const totals = calculateRevenueTotals([]);
       expect(totals).toEqual({
         quantity: 0,
         lineTotal: 0,
         freight: 0,
         unitCost: 0,
         lineCost: 0,
         lineProfit: 0,
       });
     });
   
     it('sums quantity correctly', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ quantity: 10 }),
         createMockItem({ quantity: 5 }),
         createMockItem({ quantity: 15 }),
       ];
       const totals = calculateRevenueTotals(items);
       expect(totals.quantity).toBe(30);
     });
   
     it('sums lineTotal correctly', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ lineTotal: 1000 }),
         createMockItem({ lineTotal: 2000 }),
       ];
       const totals = calculateRevenueTotals(items);
       expect(totals.lineTotal).toBe(3000);
     });
   
     it('sums freight, treating null as 0', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ freight: 100 }),
         createMockItem({ freight: 50 }),
       ];
       const totals = calculateRevenueTotals(items);
       expect(totals.freight).toBe(150);
     });
   
     it('sums unitCost, skipping null values', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ unitCost: 60 }),
         createMockItem({ unitCost: null }),
         createMockItem({ unitCost: 40 }),
       ];
       const totals = calculateRevenueTotals(items);
       expect(totals.unitCost).toBe(100);
     });
   
     it('sums lineCost, skipping null values', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ lineCost: 500 }),
         createMockItem({ lineCost: null }),
       ];
       const totals = calculateRevenueTotals(items);
       expect(totals.lineCost).toBe(500);
     });
   
     it('sums lineProfit, skipping null values', () => {
       const items: SalesRevenueLineItemDto[] = [
         createMockItem({ lineProfit: 200 }),
         createMockItem({ lineProfit: null }),
         createMockItem({ lineProfit: 300 }),
       ];
       const totals = calculateRevenueTotals(items);
       expect(totals.lineProfit).toBe(500);
     });
   });
   
   function createMockItem(overrides: Partial<SalesRevenueLineItemDto> = {}): SalesRevenueLineItemDto {
     return {
       quotationId: 'q1',
       quotationCode: 'QT001',
       quotationDate: '2026-05-01',
       confirmedAt: '2026-05-02',
       revenueDate: '2026-05-02',
       customerName: 'Test Customer',
       customerAddress: null,
       contactPhone: null,
       deliveryAddress: null,
       deliveryPhone: null,
       freight: 0,
       isFirstLineOfQuotation: true,
       productName: 'Test Product',
       specification: null,
       unitName: 'pcs',
       length: null,
       width: null,
       thickness: null,
       density: null,
       sheetCount: null,
       quantity: 1,
       unitPrice: 100,
       lineTotal: 100,
       unitCost: null,
       lineCost: null,
       lineProfit: null,
       ...overrides,
     };
   }
   EOF
   ```
   Expected: FAIL with `Cannot find name 'calculateRevenueTotals'`

2. Run test to verify it fails
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```

3. Write minimal implementation
   Add this function to `sales-revenue-detail-page.tsx` before the component export:
   ```typescript
   interface RevenueTotals {
     quantity: number;
     lineTotal: number;
     freight: number;
     unitCost: number;
     lineCost: number;
     lineProfit: number;
   }

   function calculateRevenueTotals(items: SalesRevenueLineItemDto[]): RevenueTotals {
     return {
       quantity: items.reduce((sum, item) => sum + item.quantity, 0),
       lineTotal: items.reduce((sum, item) => sum + item.lineTotal, 0),
       freight: items.reduce((sum, item) => sum + item.freight, 0),
       unitCost: items.reduce((sum, item) => sum + (item.unitCost ?? 0), 0),
       lineCost: items.reduce((sum, item) => sum + (item.lineCost ?? 0), 0),
       lineProfit: items.reduce((sum, item) => sum + (item.lineProfit ?? 0), 0),
     };
   }

   export { calculateRevenueTotals };
   ```

4. Run tests to verify they pass
   ```bash
   cd frontend && npm run test -- sales-revenue-detail-page.test.tsx
   ```
   Expected: PASS (all tests)

5. Commit
   ```bash
   git add frontend/src/pages/reports/sales-revenue-detail-page.tsx \
           frontend/src/pages/reports/sales-revenue-detail-page.test.tsx
   git commit -m "feat: add calculateRevenueTotals utility for sales revenue detail

   Adds function to compute aggregate totals (quantity, lineTotal, freight,
   unitCost, lineCost, lineProfit) from line item array. Includes comprehensive
   test coverage for edge cases (nulls, empty arrays).
   
   Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>"
   ```

## Verification
- All tests pass: `npm run test -- sales-revenue-detail-page.test.tsx`
- Function correctly exports and is importable
- Logic handles null values correctly per test assertions

## Exit Criteria
- ✅ `calculateRevenueTotals` function is implemented and exported
- ✅ All 7 test cases pass
- ✅ Function handles empty arrays and null values correctly
