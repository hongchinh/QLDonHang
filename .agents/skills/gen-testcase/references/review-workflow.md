# Review Workflow

AI-based quality review for generated test cases. Cross-checks coverage, rule compliance, and quality against the BD source. Produces an actionable report, then optionally applies fixes.

This workflow is portable — it depends only on markdown files and Claude's reasoning. No external scripts, no dependencies on other skills.

> **Settings reference**: Thresholds used in this workflow are defined in `settings.yaml`
> (root of gen-testcase skill) under the `review` section.

## Arguments

- `{module}` — module name under `docs/testcases/`

## When to run

- **After `generate`** — before exporting to xlsx, to catch gaps and rule violations early
- **Standalone** — anytime after TCs exist, to audit quality
- **Before handoff** — before sharing with tester/reviewer

## Inputs

1. **Config**: `docs/testcases/{module}/config.yaml`
2. **BD source**: `docs/testcases/{module}/input/BD.md`
3. **BD images**: `docs/testcases/{module}/input/images/*.png` (read multimodal)
4. **Rules** (priority): `input/rules.md` → `docs/testcases/rules.md` → `references/templates/rules-default.md`
5. **GUI TCs**: `docs/testcases/{module}/output/gui-testcases.md`
6. **Function TCs**: `docs/testcases/{module}/output/function-testcases.md`

If any required input is missing, stop with a clear error — do not invent data.

## Output

- **Report**: `docs/testcases/{module}/output/review-report.md`
- **Console summary**: counts + top issues + next-step prompt

## Steps

### Step 0: Assess Review Depth

Before executing the full review, check screen complexity:

**Light review conditions** (ALL must be true — thresholds from `settings.review.light_mode`):
- `BD.md` is less than `settings.review.light_mode.bd_max_lines` lines
- Total BD items ≤ `settings.review.light_mode.bd_max_items`
- Screen has no validation rules (read-only or view-only screen)

**If light review conditions are met:**
1. Skip Steps 4-6 (Rule Compliance detail, Quality deep-check, Duplicate detection)
2. Execute only: Step 1 (Load Context), Step 2 (Parse TCs), Step 3 (Cross-Check Coverage)
3. Print an inline summary instead of generating `review-report.md`:
   ```
   === Light Review: {screen_name} ({screen_id}) ===
   GUI TCs: {n}  |  Function TCs: {n}
   BD items covered: {n}/{total} ({pct}%)
   
   {any coverage gaps listed as bullet points, or "No gaps found."}
   ```
4. If coverage gaps found, ask user whether to add missing TCs before proceeding to export
5. If no gaps, proceed directly to export (no gate needed)

**If conditions are NOT met:** Execute the full review workflow (Steps 1-8) as currently documented.

### Step 1: Load Context
Read all 6 inputs above. Extract from config: `screen_name`, `screen_id`, `mode`.

### Step 2: Parse TCs
For each markdown TC file, extract:
- TC ID (e.g. `GUI_A.1_1`, `FUNC_A.2.1_1`, `FUNC_A.3.4_2`)
- Section/subsection (A.1, A.2.1, A.3.4, etc.)
- Title
- Whether Precondition/Expected fields are present and non-empty
- **For both GUI and Function TCs:** check Steps field — required for both formats.
- **For GUI TCs:** expect 4 fields (Title, Precondition optional, Steps required, Expected). Flag missing Steps as a critical issue.

### Step 3: Cross-Check Against BD

**Coverage check** — for every BD item (1.0 → N.0):
- Is there at least one GUI TC touching this item? (check by field name appearing in title/steps/expected)
- Is there at least one Function TC touching this item?
- For items with validation rules (required, maxlength, format, business rules), is each specific validation covered?
- For items that differ per group variant (e.g. A2 required vs R2 optional), is there a TC **per group** covering the variant behavior?

**Group-scoped coverage** (applies when `## Group:` headings are present in the TC markdown):
- Parse the list of groups from the markdown headings (e.g. A2, R2).
- For each BD item that specifies group-specific behavior (e.g. "A2 required, R2 optional"), verify that:
  - The A2 group has a TC covering the required-field behavior
  - The R2 group has a TC covering the optional-field behavior
- Missing per-group TCs for group-variant BD items → **critical issue**.
- If the TC markdown has no `## Group:` headings but the BD clearly has group variants → **warning** (suggest adding groups).

Items with zero coverage → **critical issue**.
Items with partial coverage (has TC but specific validation missing) → **warning**.

**Button validation grouping** — if BD lists validations triggered by a button (e.g. 更新), all per-field validations should be grouped under that button's section in Function TCs (Rule 13).

**Navigation completeness** — if BD mentions a button opens another screen, there should be exactly one Navigation TC per destination (Rule 12).

### Step 4: Check Rule Compliance

Load the rules file (from Step 1). Parse ALL numbered rules from the file.

**Parsing strategy:**
- Identify rules by their numbered format: `N. **Rule name**: description` or `N. **Rule name**:` followed by description text
- Extract both the rule number/name and the description for each rule
- Do NOT limit to a specific range of rule numbers — check every rule found in the file

**For each rule found**, assess whether the generated TCs comply:
- Scan TC titles, steps, expected results, and structure for violation patterns
- The rule description itself defines what to check — do not rely on a separate violation table
- Flag violations with the rule number and a brief explanation

**Common violation patterns** (non-exhaustive reference — apply to any rule that matches):
- Title format issues (too long, missing what/how/where)
- Missing per-field or combined validation TCs
- TCs that should be merged (same field, non-UI-component)
- Missing validation scenarios mentioned in BD
- Hardcoded DB values instead of generic references
- Incorrect navigation TC structure
- Format violations (tables instead of key-value, wrong language)

Report each violation with: rule number, affected TC ID(s), description, and suggested fix.

### Step 5: Check Quality

For each TC, verify (type-aware checks):

**GUI TCs (4-field format):**
- **Title**: present, non-empty, descriptive
- **Precondition**: if present, describes state meaningfully
- **Steps**: present, non-empty, describes the user action being verified (terse, Vietnamese)
- **Expected**: includes specific expected result

**Function TCs (4-field format):**
- **Precondition**: describes state before test (not just "open form")
- **Steps**: specific, reproducible, not vague
- **Expected**: includes exact message text from BD when BD specifies one (e.g. 「施術年月は必須です。」)

Flag vague/incomplete TCs as **warnings**.

### Step 6: Detect Duplicates

Find TCs that:
- Test the same field with the same validation scenario
- Could be merged per Rule 3 (same column, non-UI-component)
- Are exact duplicates (same title, same steps)

**3-level ID awareness**: TCs in different sub-subsections (e.g. A.2.1 vs A.2.2) test different operations (Create vs Update) — do NOT flag them as duplicates even if they test similar fields. Duplicates must be within the same subsection.

Flag as **suggestions**.

### Step 7: Generate Review Report

Use `references/templates/review-report-template.md` as the structure. Fill:
- **Summary**: counts, coverage %, rule compliance %, issue counts
- **Critical issues**: must-fix items with specific TC IDs and suggested fixes
- **Warnings**: should-fix items
- **Suggestions**: nice-to-have improvements
- **Fix list**: actionable checklist that can be auto-applied

Save to `docs/testcases/{module}/output/review-report.md`.

### Step 8: Print Console Summary

Format:
```
=== Review: {screen_name} ({screen_id}) ===
GUI TCs: {n}  |  Function TCs: {n}
BD items covered: {n}/{total} ({pct}%)
Rule compliance: {pct}%

Issues:
  Critical: {n}  |  Warning: {n}  |  Suggestion: {n}

Top 3 critical issues:
  1. {issue summary}
  2. {issue summary}
  3. {issue summary}

Report: docs/testcases/{module}/output/review-report.md

Next: apply fixes, or proceed to export.
```

If **no issues found** (0 critical, 0 warnings): print `"Review passed — ready to export."` and stop.

If **issues found** and running standalone (not via `run` pipeline):
1. Open `review-report.md` to see the full fix list
2. Edit `gui-testcases.md` or `function-testcases.md` directly (each TC is a `### {id}` block)
3. Re-run `/gen-testcase review {module}` to verify fixes — repeat until review passes
4. Then run `/gen-testcase export {module}`

> ⚠️ **Note on manual edits**: Changes made to `.md` files are **preserved** on the next `review` or `export` run. However, re-running `generate` will **overwrite** them (a `.bak.md` backup is created first).

## Fix application (optional second phase)

When invoked from `run` pipeline, after generating the report:

- If verdict is **PASS** (0 critical, 0 warnings): skip fix application, print
  `"Review passed — no fixes needed. Proceeding to export."` and continue to export.
- If verdict is **NEEDS FIXES** or **BLOCKED**: ask user via `Question Tool`:

> "Review found {critical} critical + {warning} warnings. Apply suggested fixes?"

Options: **Yes, apply all** | **Yes, apply critical only** | **No, I'll fix manually** | **Show me the report first**

If user chooses to apply:

> ⚠️ **No backup**: Fix application edits `gui-testcases.md` and `function-testcases.md` in-place. No `.bak.md` copy is created — changes are granular per-TC edits, not full-file overwrites.

1. Read `review-report.md` fix list
2. For each fix, locate the target TC in `gui-testcases.md` or `function-testcases.md`
3. Apply the specific edit:
   - **Add TC**: insert new TC at correct section with next available sequential ID
   - **Update TC**: modify title/steps/expected as specified
   - **Merge TCs**: combine into single TC, remove duplicates
   - **Remove TC**: delete entire TC block
4. After all fixes applied, print summary: `Applied {n} fixes. {m} skipped (manual review needed).`
5. After fixes applied, use `Question Tool`:
   > "{n} fixes applied. Re-run review to verify?"

   Options:
   - **Yes, re-review (Recommended)** — re-run Steps 1-8 of this workflow. The new report will overwrite `review-report.md`. Expect fewer issues.
   - **No, proceed to export** — trust fixes and continue pipeline

   **Loop guard:** Maximum `settings.review.max_revert_cycles` re-review cycles to prevent infinite loops. After reaching the limit, always proceed to export regardless of remaining issues. Print:
   `"Max re-review cycles reached. Proceeding to export. Check review-report.md for remaining items."`

**Do not** blindly apply fixes that require human judgment (e.g. "rephrase for clarity"). Mark those as `manual` in the fix list so user knows to check.

## Notes

- **Idempotent**: running review twice on same TCs should produce same report
- **No side effects**: review itself never modifies TCs (only fix application does)
- **Portable**: no external tool dependencies — works in any project that has gen-testcase installed
- **Multimodal**: read BD images to verify layout TCs reference correct UI elements
