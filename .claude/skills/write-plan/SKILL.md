---
name: write-plan
description: Create detailed, execution-ready implementation plans for complex or high-risk changes without coding. Use when scope is large, requirements are mostly known, and work should be broken into validated phases before execution.
---

# Write Plan

## Overview

Produce a complete, self-contained implementation plan that can be executed by `execute-plan` with minimal ambiguity.

This skill is for planning only:

- Do not implement code
- Do not modify production files (except plan artifacts)

## Workflow

### Step 1: Contextualize

Read project context when relevant: `CLAUDE.md`, `docs/SUMMARY.md`, plus the code areas relevant to the requested change.

Capture:

- Existing patterns to follow
- Constraints and dependencies
- Risks, assumptions, and unknowns

### Step 2: Clarify Requirements

Ask clarifying questions to resolve any ambiguity in the request. Focus on:

- Scope and boundaries
- Success criteria
- Constraints and non-goals
- Priorities and trade-offs

Rules:

- If requirements are already clear or come from a prior `brainstorm` context, skip the confirmation step.
- Use `AskUserQuestion` for gathering answers.
- State assumptions explicitly in `SUMMARY.md`. If multiple interpretations of the request exist, list them and ask — never pick silently.

### Step 3: Initialize Plan Artifacts

1. Create: `docs/plans/YYMMDD-HHmm-<plan-slug>/`
2. Create:
   - `SUMMARY.md`
   - one phase file per implementation phase with naming convention `phase-XX-<name>.md`
3. Add `research/` only if needed.

Use the local clock for the `YYMMDD-HHmm` timestamp.

### Step 4: Define Strategy and Phases

Design a phased strategy that is safe and verifiable.

Each phase should have:

- A clear objective
- Complexity/risk level: `S`, `M`, `L`, or `XL`
- Ordered tasks
- Verification commands
- Exit criteria

Granularity rule:

- Tasks should be small, concrete, and typically 2-10 minutes each.

**TDD enforcement:** Every task that produces code MUST follow this cycle — no exceptions:

1. Write the failing test
2. Run test to verify it fails (with expected error)
3. Write minimal implementation to make it pass
4. Run tests to verify they pass
5. Commit

Write each of these as a separate numbered step inside the task. Steps that skip this cycle (e.g., "implement X and add tests") are plan failures.

**Greenfield exception:** If the project has no test framework yet, the first phase MUST be "Set up test infrastructure" — install framework, configure runner, write one smoke test to confirm the setup works. All subsequent phases then follow TDD normally. Never plan feature tasks without a working test runner.

### Step 5: Research (Only if Needed)

Research is optional and should be proportional to uncertainty.

Preferred order:

1. Existing project docs and code
2. Existing skills and local references
3. External references (only if available in the current environment)

If external research capability is unavailable, proceed with local evidence and explicitly list assumptions and open questions.

Document findings in:

- `docs/plans/YYMMDD-HHmm-<plan-slug>/research/<topic>.md`

### Step 6: Write Plan Content

#### `SUMMARY.md` structure

```markdown
# <Plan Title>

## Goal
One-paragraph objective.

## Scope
- In scope: ...
- Out of scope: ...

## Assumptions
- ...

## Risks
- ...

## Phases
- [ ] Phase 01 — <name> (S/M/L/XL) — `phase-01-<name>.md`
- [ ] Phase 02 — <name> (S/M/L/XL) — `phase-02-<name>.md`

## Final Verification
Commands to run after all phases pass.

## Rollback / Recovery
How to undo the change if something goes wrong.
```

#### `phase-XX-<name>.md` structure

```markdown
# Phase XX — <name>

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** S | M | L | XL

## Objective
What this phase achieves.

## Files
- path/to/file1.ext
- path/to/file2.ext

## Tasks

For each code-producing task, use this TDD structure:

1. Write the failing test — `<test command>` / Expected: FAIL with `<error>`
2. Run test to verify it fails
3. Write minimal implementation
4. Run tests to verify they pass — `<test command>` / Expected: PASS
5. Commit — `git commit -m "<message>"`

## Verification
- Command 1
- Command 2

## Exit Criteria
- ...
```

### Step 7: Review and Refine

Before presenting the plan, run this self-review checklist:

**Structure & completeness:**
- Paths are exact and consistent
- Phase order is logical
- Tasks are actionable (no vague steps)
- Verification is defined for each phase
- Risks/assumptions are explicit
- Plan is executable without hidden context

**Spec coverage:** Skim each requirement from the spec or brainstorm output. Can you point to a task that implements it? List any gaps and add missing tasks.

**Placeholder scan:** Search for these plan failures — find and fix all before presenting:
- "TBD", "TODO", "implement later", "fill in details"
- "Add appropriate error handling" / "handle edge cases" (without specifics)
- "Write tests for the above" (without actual test command or structure)
- "Similar to Phase N / Task N" (repeat the detail — executor may read out of order)
- Steps that describe what to do without showing how

**Type consistency:** Do function names, method signatures, and type names used in later phases match what was defined in earlier phases? A method called `GetQuotations()` in Phase 2 but `FetchQuotations()` in Phase 4 is a bug in the plan. Fix all mismatches inline.

Fix any issues inline before presenting. Then present for user review.

If multiple viable approaches exist, present options via `AskUserQuestion`:

- **Confirm**: approve current plan for execution
- **Validate**: refine via additional clarifying questions

### Step 8: Handoff

When approved, end with:

> Plan `<relative_path_to_plan>/SUMMARY.md` is ready.
> Use `/clear` and then invoke the `execute-plan` skill with `<relative_path_to_plan>/SUMMARY.md` to execute it.

## Rules

- Never automatically implement or execute the code change in the same session — finish when planning is complete and ready for user review.
- Prefer explicit file paths and concrete commands.
- Align with project standards and existing architecture.
- Keep plans self-contained and deterministic.
- **Plan the minimum viable change:** No speculative phases, no "just in case" abstractions, no flexibility that wasn't requested. If a plan can be 3 phases instead of 6, make it 3. Every task should trace directly to a stated requirement.
- If the write-plan request comes from a `brainstorm` session, skip steps already covered (context gathering, requirement clarification, research). Start from Step 3 (Initialize Plan Artifacts) using the brainstorm output as context.
