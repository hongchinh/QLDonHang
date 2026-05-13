# Test Case Review Report — {screen_name}

> Screen ID: {screen_id} | Mode: {mode}
> Generated: {date}
> Reviewer: AI (gen-testcase review workflow)

## Summary

| Metric | Value |
|--------|-------|
| GUI TCs | {gui_count} |
| Function TCs | {func_count} |
| Total TCs | {total_count} |
| BD items | {bd_item_count} |
| Items covered | {covered_count}/{bd_item_count} ({coverage_pct}%) |
| Rule compliance | {compliance_pct}% |
| Critical issues | {critical_count} |
| Warnings | {warning_count} |
| Suggestions | {suggestion_count} |

**Verdict:** {PASS | NEEDS FIXES | BLOCKED}

<!--
PASS      — 0 critical, 100% coverage, high compliance. Ready to export.
NEEDS FIXES — Some warnings/suggestions but no blockers. User judgment.
BLOCKED   — Critical issues. Fix before export.
-->

## Coverage Matrix

One row per BD item. Mark GUI/Function coverage and flag gaps.

| # | BD Item | Type | GUI TCs | Function TCs | Status |
|---|---------|------|---------|--------------|--------|
| 1.0 | {item_name} | {type} | A.1_1 | B.2_1 | OK |
| 2.0 | {item_name} | {type} | B.1_1 | C.1_1, C.3_1-6 | OK |
| 3.0 | {item_name} | {type} | A.1_2 | H.1_1 | WARN: missing edge case X |
| 4.0 | {item_name} | {type} | — | — | CRIT: no coverage |
| ... | ... | ... | ... | ... | ... |

## Group Coverage (omit section if no groups)

<!--
Include this section only when the module uses ## Group: headings.
One row per (BD item × group) that has group-specific behavior.
-->

| BD Item | Group | Expected behavior | GUI TCs | Function TCs | Status |
|---------|-------|-------------------|---------|--------------|--------|
| {item} | A2 | Required field | A.1_2 | C.1_3 | OK |
| {item} | R2 | Optional field | A.1_3 | C.1_4 | OK |
| {item} | A2 | Required field | — | — | CRIT: missing A2 TC |

## Critical Issues (must fix before export)

### CRIT-1: {Short title}
- **Category:** {coverage | rule | quality}
- **BD item/rule:** {reference}
- **Affected TCs:** {TC IDs or "none (missing)"}
- **Problem:** {clear description}
- **Suggested fix:** {specific, actionable — what to add/change}
- **Auto-applicable:** {yes | no (needs human judgment)}

### CRIT-2: {...}

## Warnings (should fix)

### WARN-1: {Short title}
- **Category:** ...
- **Affected TCs:** ...
- **Problem:** ...
- **Suggested fix:** ...
- **Auto-applicable:** ...

## Suggestions (nice to have)

### SUGG-1: {Short title}
- **Category:** ...
- **Problem:** ...
- **Suggested fix:** ...

## Rule Compliance Detail

| Rule | Description | Violations | TC IDs |
|------|-------------|-----------|--------|
<!-- One row per rule found in the rules file. Include ALL rules, not just a fixed set. -->

## Fix List (for auto-apply)

Checklist format — the fix application phase reads this to apply changes.

- [ ] **FIX-1** [CRIT-1] Add B.2_3 for checkbox X — section B.2 after B.2_2
- [ ] **FIX-2** [CRIT-2] Add C.1_9 for field Y required validation — section C.1
- [ ] **FIX-3** [WARN-1] Update C.2_6 steps — add exact error message from BD
- [ ] **FIX-4** [WARN-2] Merge B.4_2 and B.4_3 (same column, non-UI-component)
- [ ] **FIX-5** [SUGG-1] Rephrase title of D.1_1 for clarity — **manual** (needs human judgment)

## Next Steps

1. Review this report
2. Choose fix strategy:
   - Apply all auto-applicable fixes → continue to export
   - Apply critical only → review warnings manually → export
   - Skip all fixes → fix manually → re-run review
3. Re-run review after fixes to verify
4. Proceed to export when verdict is PASS or acceptable NEEDS FIXES
