# Execution Report — Auto-save nháp báo giá mới

**Plan:** `docs/plans/260529-2026-quotation-draft/SUMMARY.md`
**Executed:** 2026-05-29
**Mode:** Batch

## Phases

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 01 — Core hook | ✅ Complete | 20/20 tests pass |
| Phase 02 — Form integration | ✅ Complete | 3/3 integration tests pass |

## Files Changed

| File | Action |
|------|--------|
| `frontend/src/features/quotations/use-quotation-draft.ts` | Created |
| `frontend/src/features/quotations/use-quotation-draft.test.ts` | Created |
| `frontend/src/pages/quotations/quotation-form-page.tsx` | Modified |
| `frontend/src/pages/quotations/quotation-form-page.draft.test.tsx` | Created |

## Verification Commands Run

```
npx vitest run src/features/quotations/use-quotation-draft.test.ts  → 20 passed
npx vitest run src/pages/quotations/quotation-form-page.draft.test.tsx  → 3 passed
npx tsc --noEmit  → no errors
npx vitest run  → 189 passed (1 pre-existing failure in useNotificationHub unrelated to these changes)
```

## Deviations from Plan

### 1. `vi.useFakeTimers({ shouldAdvanceTime: true })` in hook tests
**Reason:** The plan's `vi.useFakeTimers()` causes `waitFor`'s internal polling `setTimeout` to be frozen by fake timers, resulting in test timeout. `shouldAdvanceTime: true` lets `waitFor` poll via real wall-clock time while `vi.advanceTimersByTime()` still controls the debounce timer.

### 2. `isDirtyRef` pattern in `useQuotationDraft`
**Reason:** RHF v7's `formState` Proxy only tracks `isDirty` correctly when accessed during React's render phase (to register the subscription). Accessing `form.formState.isDirty` solely inside a `setTimeout` callback returns a stale/untracked value. Fix: read `isDirty` in the hook body each render and mirror into a ref that the timeout closure reads.

### 3. Async `vi.mock` factory for `line-items-grid` in integration test
**Reason:** The plan used `React.forwardRef(...)` inside a synchronous `vi.mock` factory. Vitest hoists `vi.mock` factories before ES module imports are initialized, so eagerly calling `React.forwardRef()` in the factory body throws a TDZ `ReferenceError`. Converted to `async () => { const { forwardRef } = await import('react'); ... }`.

## Residual Risks / Follow-ups

- Manual QA steps from the plan still need verification in a running browser (dev server).
- Pre-existing failing test in `src/hooks/useNotificationHub.test.ts` is unrelated to this work.
