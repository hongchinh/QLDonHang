---
name: gen-testcase
description: "Generate structured test cases from Business Design (BD) files. Use when the user has BD source file(s) (.xlsx or .md), wants to create GUI/Function test cases, needs QA test case generation, mentions tester workflow, or asks to export test cases to xlsx template. Covers the full pipeline: init → convert → generate → review → export."
argument-hint: "init|convert|generate|review|export|run {module}"
---

# Gen-Testcase — BD Source → Test Cases

Generate structured test cases from BD source files (`.xlsx` or `.md`). Pipeline: init → convert → generate → review (optional) → export.

## Default (No Arguments)

If invoked without arguments, use `Question Tool` to present operations:

| Operation | Description |
|-----------|-------------|
| `init {module}` | Initialize module directory with config |
| `convert {module}` | Convert configured BD source → BD.md + images |
| `generate {module}` | AI-generate GUI + Function test cases |
| `review {module}` | AI-review generated TCs for coverage, rules, quality |
| `export {module}` | Export markdown TCs → DJP xlsx |
| `run {module}` | Run full pipeline with optional review gate |

Present via `Question Tool` with header "Gen-Testcase", question "Select operation:".

## Subcommands

| Subcommand | Reference | Purpose |
|------------|-----------|---------|
| `init {module}` | `references/init-workflow.md` | Setup module dir + config |
| `convert {module}` | `references/convert-workflow.md` | Configured BD source → BD.md + images |
| `generate {module}` | `references/generate-workflow.md` | AI generate GUI/Function TCs |
| `review {module}` | `references/review-workflow.md` | AI-review TCs (coverage, rules, quality), produce actionable report |
| `export {module}` | `references/export-workflow.md` | Markdown TCs → DJP xlsx (auto-increments version if prior export exists) |
| `run {module}` | All workflows sequentially | Full pipeline with review gate |

## Global Settings

Skill-level defaults (thresholds, keywords, column maps, sheet names, TC ID prefixes, etc.)
are centralized in `settings.yaml` at the root of this skill directory. Scripts load it
automatically; workflows reference values via `settings.{section}.{key}` notation.

To customize behavior (e.g. change BD summary threshold, add new field type keywords,
modify default xlsx column layout), edit `settings.yaml` — no need to touch scripts or workflows.

## Routing

Parse `$ARGUMENTS`:
- Starts with `init` → Load `references/init-workflow.md`
- Starts with `convert` → Load `references/convert-workflow.md`
- Starts with `generate` → Load `references/generate-workflow.md`
- Starts with `review` → Load `references/review-workflow.md`
- Starts with `export` → Load `references/export-workflow.md`
- Starts with `run` → Execute pipeline with review gate (see below)
- Empty/unclear → Question Tool

## Run pipeline (with review gate)

When `run {module}` is invoked, execute phases in order with user gates at key checkpoints. Review is **optional** — the default recommendation is to run it, but the user decides.

**Smart skip — init phase:**
When running `run {module}`, check before executing init:
- If `docs/testcases/{module}/config.yaml` exists AND is non-empty
- AND `docs/testcases/{module}/input/` directory exists
- AND `docs/testcases/{module}/output/` directory exists
→ **Skip init entirely**. Print: `Module already initialized. Skipping init.`
Only run init if any of these are missing.

```
init → convert → generate
                    │
                    ▼
         [GATE 1] Question Tool:
         "Review generated TCs before export?"
         ├─ Yes (Recommended) → load review-workflow.md → produce review-report.md
         │                       │
         │                       ▼
         │           [GATE 2] Question Tool (only if issues found):
         │           "Apply suggested fixes?"
         │           ├─ Yes, apply all   → AI edits TCs per fix list → export
         │           ├─ Yes, critical only → AI applies critical fixes → export
         │           ├─ Show me report first → pause, wait for user decision
         │           └─ No, I'll fix manually → export
         │
         └─ No, export directly → export
```

**Gate behavior:**
- Gate 1 uses `Question Tool` with options: "Yes, review (Recommended)" | "No, export directly"
- Gate 2 only fires if review found critical issues or warnings (skip if verdict is PASS)
- Gate 2 options adapt to findings (e.g. don't offer "critical only" if no critical issues)
- In all cases, final step is `export` unless user explicitly aborts

**Standalone `review`:** Runs independently without gates — produces report and prints summary, does not prompt for fix application. Use this mode when auditing existing TCs.

## Shared Context

- **Module dir**: `docs/testcases/{module}/`
- **Config**: `docs/testcases/{module}/config.yaml`
- **Input BD source**: Multiple files from `bd_files` list in config. If empty, `convert` auto-detects. Other formats: convert to `.xlsx` or `.md` first.
- **Input BD markdown**: `docs/testcases/{module}/input/BD.md`
- **Input images**: `docs/testcases/{module}/input/images/`
- **BD summary** (optional): `docs/testcases/{module}/input/bd-summary.json` — structured summary extracted by convert, used by generate for token-efficient planning
- **Input context** (optional): `docs/testcases/{module}/input/context/` — enrichment data such as screenshots, Figma notes, or Jira-derived context
- **Rules**: `docs/testcases/{module}/input/rules.md` (fallback: `docs/testcases/rules.md`)
- **Output GUI TCs**: `docs/testcases/{module}/output/gui-testcases.md`
- **Output Function TCs**: `docs/testcases/{module}/output/function-testcases.md`
- **Output review report**: `docs/testcases/{module}/output/review-report.md`
- **Output xlsx**: `docs/testcases/{module}/output/{project_name}_{screen_name}_Testcase_v{version}.xlsx`
- **Group detection**: `group_detection_patterns` in config.yaml (optional) — custom patterns for auto-detecting groups in BD
- **xlsx template** (user-provided):
  - `references/templates/DJP_TestCase_Template_Ver1.0.xlsx` — 7-sheet template with placeholders and TC data rows (Cover, Changed History, Test Report, Common, GUI, FUNC, Screen Layout)
  - Swap this file to change export layout without editing workflow code
- **TC templates**: `references/templates/gui-testcase-template.md` (4-field: Title, Precondition, Steps, Expected), `references/templates/function-testcase-template.md` (4-field: Title, Precondition, Steps, Expected)
- **TC ID format**:
  - GUI sheet: `GUI_{letter}.{section}_{seq}` — e.g. `GUI_A.1_1`, `GUI_A.2_3`
  - FUNC sheet: `FUNC_{letter}.{section}[.{subsection}]_{seq}` — e.g. `FUNC_A.1_1`, `FUNC_A.2.1_1`, `FUNC_A.3.4_2` (supports 3-level nesting)
- **xlsx column layout (per-sheet)**:
  - GUI sheet: 5 data columns (TC ID, Pre-conditions, Title, Steps, Expected Result)
  - FUNC sheet: 5 data columns (TC ID, Pre-conditions, Title, Steps, Expected)

## Security

- No hardcoded project names — all metadata from config.yaml
- No external network calls
- File operations scoped to module directory

## Dependencies

- **Python packages**: `openpyxl>=3.1.0`, `pyyaml>=6.0` — installed automatically via `scripts/requirements.txt`
- No external skill dependencies

## Python Scripts

Located in `scripts/` — called by convert and export workflows.

| Script | Purpose | CLI |
|--------|---------|-----|
| `convert-bd-to-md.py` | BD source (`.xlsx`/`.md`) → BD.md + images | `--input --output --images-dir` |
| `export-tc-to-xlsx.py` | MD TCs + template → xlsx | `--config --template --gui --function [--common] --output` |
| `extract-bd-summary.py` | BD.md → bd-summary.json (planning aid) | `--input --output` |

Scripts are self-contained (no inter-file imports). Install deps: `pip install -r scripts/requirements.txt`

## Rules

- Do not hardcode project names, screen IDs, or metadata — all values come from `config.yaml`.
- Do not make external network calls; all operations are local file I/O.
- Scope all file operations to the module directory (`docs/testcases/{module}/`); never read or write outside it except for skill references and templates.
- Use Vietnamese for TC content (titles, steps, expected results); keep skill instructions and config in English.
- Do not duplicate design rules in workflows or templates — the rules file (`rules.md` / `rules-default.md`) is the single source of truth.
- Before overwriting existing TC output files (`gui-testcases.md`, `function-testcases.md`), back them up by copying to `{filename}.bak.md` in the same directory so manual edits are not lost. Exception: review fix application edits TCs in-place without backup — changes are granular (per-TC), not full-file overwrites.
- Do not infer business meaning during the `convert` step — preserve raw cell text; structural conversion only.
- Always use `Question Tool` at pipeline gates and user-facing decisions; never ask questions in plain text.
