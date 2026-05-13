# Phase 03: Verification and Polish

## Objective

- Verify the keyboard workflow end to end and clean up any small issues found during implementation.

## Preconditions

- Phase 1 and Phase 2 are complete.
- Frontend dependencies are installed.

## Tasks

1. Run frontend typecheck.
2. Run available frontend tests.
3. If typecheck reveals typing issues around `forwardRef`, adjust exported types without changing behavior.
4. Manually validate keyboard navigation on the quotation create page.
5. Manually validate keyboard navigation on the quotation edit page if data is available.
6. Confirm there is no browser Save Page prompt when pressing `Ctrl+S`.
7. Confirm submit button and existing action buttons still work.
8. Document any residual manual-test limitations in the final execution report.

## Verification

- Commands:
  - `cd frontend && npm run typecheck`
  - `cd frontend && npm run test`
- Expected results:
  - Typecheck passes.
  - Tests pass, or any pre-existing failures are documented with evidence.
  - Manual keyboard checks match the requested behavior.

## Exit Criteria

- No new TypeScript errors.
- No new failing tests attributable to this change.
- User can complete header-entry flow with `Enter` / `Shift+Enter`.
- User can save with `Ctrl+S`.
- Customer and product autocomplete dropdowns still use `Enter` to select while open.
