# Phase 03: Verification and Guide Polish

## Objective

- Verify the row-major navigation and keep the visible keyboard guide aligned with the final behavior.

## Preconditions

- Phase 1 and Phase 2 are complete.
- Frontend dependencies are installed.

## Tasks

1. Review the footer keyboard guide in `LineItemsGrid`.
2. Update labels only if needed so they accurately describe row-major navigation:
   - `Enter` next cell
   - `Shift+Enter` previous cell
   - `Insert` add row
   - `Ctrl+Delete` delete row
   - `Ctrl+S` save
3. Run lint.
4. Run typecheck.
5. Run tests.
6. Run build.
7. Perform or document manual QA coverage for the row-major checklist in `SUMMARY.md`.
8. If any automated verification fails due to sandbox `spawn EPERM`, rerun the same command with escalation and document that during execution.

## Verification

- Commands:
  - `cd frontend && npm run lint`
  - `cd frontend && npm run typecheck`
  - `cd frontend && npm run test`
  - `cd frontend && npm run build`
- Expected results:
  - Lint passes.
  - Typecheck passes.
  - Tests pass.
  - Build passes.

## Exit Criteria

- Keyboard guide matches implemented behavior.
- Automated verification passes or any environment-only blocker is documented.
- Manual QA checklist is ready for browser validation.
