# Generate Workflow

AI-generate GUI and Function test cases from BD.md content.

> **Settings reference**: Thresholds and default section structures used in this workflow
> are defined in `settings.yaml` (root of gen-testcase skill). Read `settings.yaml` at the
> start of generation to get current values for all thresholds referenced below.

## Arguments

- `{module}` — module name (matches directory under `docs/testcases/`)

## Steps

### Step 0: Check Regeneration Scope

If TC output files already exist (`gui-testcases.md` and/or `function-testcases.md`):

Use `Question Tool` to ask:
> "Existing TCs found. What scope should be regenerated?"

Options:
- **Full regenerate (Recommended)** — regenerate all sections, backup existing files to `.bak.md`
- **Single section only** — regenerate one specific section (for example only `C. INPUT VALIDATION`)
- **Skip, keep existing** — abort generate, keep current files

**If user picks "Single section only":**
1. Use `Question Tool` to let user pick which section (list resolved sections from Step 6.5 logic)
2. Also ask which file: GUI, Function, or both
3. Read the existing file(s)
4. Generate TCs for only the selected section
5. Replace ONLY that section in the existing file using Edit tool:
   - Find the section header (for example `## C. INPUT VALIDATION`)
   - Find the next section header or end of file
   - Replace everything between with newly generated content
6. Preserve all other sections untouched
7. Skip to Step 9 (Verify Output) — no need to regenerate other sections

**If user picks "Full regenerate":** Continue with existing workflow (Step 1 onwards).
**If user picks "Skip":** Print `Keeping existing TCs.` and exit.

**First-time generation (no existing files):** Skip this step entirely.

### Step 1: Load Config

   ```
   docs/testcases/{module}/config.yaml
   ```
   Extract: `screen_name`, `screen_id`, `mode`, `group_type`, `groups`

   **Validate required fields:**
   - `screen_name` must be non-empty → error if missing: `"config.yaml: screen_name is required. Run init or edit config."`
   - `screen_id` must be non-empty → error if missing
   - `project` must be non-empty → warn (needed for export filename)
   - `mode` defaults to `web` if empty

   If any required field is missing, stop with a clear error listing all missing fields — do not proceed with placeholders.

### Step 2: Detect Groups From BD

Advisory — do not skip:
   - If config already has `group_type` ≠ `none` and `groups` is non-empty: use config groups as authoritative; skip detection.
   - Otherwise:
     - **If `group_detection_patterns` is set and non-empty** in config: scan BD content for those specific patterns.
     - **If `group_detection_patterns` is empty** (default): use generic heuristic scanning:
       - Look for repeated structural blocks with different heading prefixes
       - Look for lines that describe per-variant behavior (e.g. "X thì bắt buộc nhập, Y không")
       - Look for block headings with code-like suffixes or prefixes
     - If markers detected: present findings to user via `Question Tool` and confirm before proceeding.
       > "Detected possible groups: {list}. Should I add ## Group: headings to the TC markdown?"
       Options: **Yes, use detected groups** | **No, flat layout**
   - If user confirms groups: note the ordered code/label list for use in Steps 7–8.

### Step 3: Load Rules

Priority order:
   - `docs/testcases/{module}/input/rules.md` (module-specific)
   - `docs/testcases/rules.md` (global fallback)
   - `references/templates/rules-default.md` (built-in fallback)

After loading, scan for numbered rules matching pattern `N. **...**: ...`.
If found rules < 3 (suspiciously low for a non-trivial screen), print warning:
  `WARNING: Only {n} rules detected in rules file. Rules must follow format:
  N. **Rule name**: description. Check that custom rules use this format.`
Continue regardless — this is advisory only.

### Step 4: Read BD Content (two-pass strategy)

   **Pass 1 — Planning (low-token)**:
   - If `docs/testcases/{module}/input/bd-summary.json` exists:
     - Read the summary JSON
     - Use it to understand: total fields, sections, validation rules, navigation targets
     - Plan which TC sections to generate and approximate TC count per section
   - If summary JSON does not exist:
     - Fall back to reading full BD.md (original behavior)
     - Skip Pass 2 differentiation — treat all content as available

   **Multi-source context:**
   - If `bd_file_notes` is set in config, read it before processing BD.md.
   - Use the notes to understand cross-file relationships:
     - Which file is the base/master
     - Which files are supplements or overrides
     - How to resolve conflicts between overlapping content
   - If notes are empty, treat all BD content as equally authoritative (merge order = priority).

   **Pass 2 — Per-section detail (on-demand)**:
   - During Steps 7-8 (actual TC generation), read targeted portions of BD.md as needed:
     - For screen layout TCs: read the sheet/section containing field definitions
     - For validation TCs: read sections flagged with validation rules in the summary
     - For navigation TCs: read sections containing navigation targets
   - Use `line_range` from the summary to read specific line ranges instead of the full file
   - Images: still read all images from `input/images/` (multimodal reads cannot be summarized)
   - Context files: still read all `.md` files in `input/context/` (supplementary, typically small)

   **Fallback**: If the summary is missing, incomplete, or the agent determines it needs more context,
   read the full BD.md. The summary is an optimization hint, not a hard constraint.

### Step 5: Clarify BD Ambiguities

Advisory — skip this step entirely if BD content is clear and complete.

**Auto-skip for read-only screens:** If all BD items have types that are display-only (Label, Text, Title, Button with no validation, Pagination) and there are no input fields (Textbox, Dropdown, Radio, Checkbox, Calendar, Text area), skip this step entirely. Print: `Read-only screen detected. Skipping ambiguity scan.`

Read-only screens have no validation rules, no input ambiguity, and no field-type uncertainty — the main ambiguity categories (1-5) do not apply.

After reading BD content (Step 4), scan for the following ambiguity categories:

| # | Category | Signal to detect | Example question |
|---|----------|-------------------|------------------|
| 1 | **Unclear validation** | Field mentions "validate" / "チェック" but no specific rule (required? format? maxlength? range?) | "Field X ghi 'validate' nhưng không rõ validate gì. Required, format, hay maxlength?" |
| 2 | **Missing error message** | Validation rule exists but no message text 「…」 specified | "Field X có rule required nhưng BD không có error message. Bỏ qua hay bạn có message cụ thể?" |
| 3 | **Ambiguous group behavior** | Groups detected (A2/R2) but a field's behavior per group is not specified | "Field X: A2 required hay optional? BD không phân biệt rõ giữa các group." |
| 4 | **Unknown navigation target** | Button mentions navigation (遷移/開く/表示) but destination screen is missing or vague | "Button '更新' có navigation nhưng BD không ghi rõ mở màn hình nào." |
| 5 | **Unclear field type** | Cannot determine if field is textbox, dropdown, radio, checkbox, calendar, etc. | "Field X: loại input nào? (textbox / dropdown / radio / checkbox)" |
| 6 | **External reference** | BD references external logic ("master data", "calculation rule", "別シート参照") without describing it | "Field X tham chiếu 'master data' nhưng BD không mô tả logic. Bạn có thông tin bổ sung?" |
| 7 | **Contradiction** | Two sections describe the same field/behavior differently | "Field X: Sheet A ghi required, Sheet B ghi optional. Cái nào đúng?" |

**Process:**

1. Scan BD.md (and images if available) for signals in the table above.
2. Collect all detected ambiguities into an internal list.
3. If the list is empty → print `BD content is clear. Skipping clarification.` and proceed to Step 6.
4. If ambiguities found:
   a. Present a brief summary: `Found {n} ambiguities in BD. Will ask {n} questions to clarify before generating TCs.`
   b. Ask each ambiguity **one at a time** via `Question Tool`. Prefer multiple-choice options when the set of valid answers is small (e.g. field type, required/optional). Use open-ended only when business context is needed.
   c. Record all answers as a `clarifications` list in memory for use during Steps 7–8 (generate).
5. When generating TCs (Steps 7–8), apply clarifications:
   - Use the clarified validation type, error message, group behavior, etc. directly in TC content.
   - If user answered "skip" or "don't know" for an item, generate TCs based on best available info and add a comment `<!-- CLARIFICATION NEEDED: {brief description} -->` in the TC output so it is flagged during review.

**Rules:**
- Do not ask about formatting, structure, or template-related issues — those are handled by rules and templates.
- Do not ask when BD is explicit — only ask when information is genuinely missing or contradictory.
- Maximum `settings.generation.max_clarification_questions` questions per generate run. If more ambiguities exist, prioritize by impact on TC quality (validations > navigation > field types > messages).
- Keep questions concise; include the BD field name/ID for easy reference.

### Step 6: Read Templates
   - `references/templates/gui-testcase-template.md`
   - `references/templates/function-testcase-template.md`

### Step 6.5: Resolve TC Sections

   Use default section structure from templates:

   **For GUI:** derive sections from `gui-testcase-template.md`:
   - Single top-level section A: `{screen_name} screen`
   - Identify which component types exist on the screen (based on BD field types). Component types include: Label, Textbox, Dropdown, Radio, Checkbox, Button, Link, Text area, Calendar, Pagination, and any other type found in BD
   - Assign subsection numbers **sequentially** starting from A.1: number the first present component type as A.1, the second as A.2, etc. Do NOT use the template's fixed A.1-A.8 mapping — this avoids numbering gaps when component types are absent
   - Example: a read-only screen with Label and Button → A.1 Label, A.2 Button (not A.1, A.5)

   **For Function:** derive sections from `function-testcase-template.md`:
   - Single top-level section A: `{screen_name} screen`
   - Default sections and sub-subsections are defined in `settings.yaml` under `generation.function_sections`, `generation.function_flow_subsections`, and `generation.validation_subsections`
   - Typically: A.1=Screen initialization, A.2=Function flow (A.2.1=Create, A.2.2=Update, A.2.3=Delete), A.3=Validation (A.3.1=Required, A.3.2=Maxlength, etc.)
   - Only generate sub-subsections for features that exist on the screen

   **Resolved section list** for each type: `[{id, title, children?}, ...]`

   **Section relevance filtering** (advisory — do not skip sections silently):
   - For each resolved section/subsection, check if BD content has relevant items
   - If a section has no relevant BD items:
     - Generate a minimal placeholder TC with a note: `<!-- No BD items found for this section -->`
     - OR skip the section entirely and note in the summary which sections were skipped
   - Use `Question Tool` if unsure whether to include a section:
     > "Section {id}. {title} has no matching BD items. Include it anyway?"
     Options: **Skip it** | **Include with placeholder**

### Step 7: Generate GUI Test Cases

   **BD content loading**: If using two-pass strategy (Step 4), read only the BD.md sections
   relevant to this TC category before generating. Use `line_range` from bd-summary.json
   to target reads. If not using two-pass, BD.md is already fully loaded.

   **Backup existing output** (if re-generating):
   - If `gui-testcases.md` already exists, copy it to `gui-testcases.bak.md`
   - If `function-testcases.md` already exists, copy it to `function-testcases.bak.md`
   - This follows the SKILL.md rule about backing up before overwriting

   - Follow gui-testcase-template.md as **format scaffold only** (section structure + TC shape)
   - Use the **resolved GUI section list** from Step 6.5 (not hardcoded template sections).
   - **GUI TCs use 4 fields:** Title (required), Precondition (optional — omit line if not needed), Steps (required), Expected (required). GUI Steps describe the user action being verified (e.g. "Quan sát label của field X", "Mở dropdown Y").
   - **Strictly apply every rule from the rules file loaded in Step 3.** The rules file is the single source of truth for design rules — do not infer rules from template comments. Pay special attention to Expected Results format (multi-line with leading bullets and reference syntax).
   - **Apply clarifications from Step 5** — use clarified validation types, error messages, group behaviors, field types, and navigation targets. If a clarification comment was noted (user answered "skip"/"don't know"), embed `<!-- CLARIFICATION NEEDED: ... -->` in the affected TC.
   - If groups were confirmed in Step 2: emit `## Group: {code} — {label}` (H2) before the first subsection of each group. TC IDs may repeat across groups. If no groups: omit group headings entirely.
   - Use Vietnamese for titles, expected results
   - Fill `{screen_name}`, `{screen_id}`, `{mode}` from config
   - Use TC ID format: `GUI_{letter}.{section}_{seq}` — e.g. `GUI_A.1_1`, `GUI_A.2_3`, `GUI_A.8_1`. Always include `GUI_` prefix, no zero-padding.
   - Use key-value format per TC (NOT markdown tables)

   **TC format example (GUI — 4 fields):**
   ```markdown
   ### GUI_A.1_1
   - **Title:** Kiểm tra tên field {field_name} mặc định
   - **Precondition:** {state — optional, omit entire line if not needed}
   - **Steps:** Quan sát label của field {field_name} trên màn hình
   - **Expected:** Hiển thị tên: {field_name}
   ```

   **Write Strategy (incremental — for large output):**

   1. Generate TCs for section A header + subsection A.1 (Textbox) first.
   2. Write the file header + section A header + A.1 content using Write tool → creates `gui-testcases.md`.
   3. For each remaining subsection (A.2, A.3, ... A.8):
      a. Generate TCs for that component-type subsection.
      b. Append to `gui-testcases.md` using Edit tool (add after the last line).
   4. After all subsections written, verify the complete file.

   **Large section handling:**
   If a single section is expected to produce > `settings.generation.large_section_tc_threshold` TCs (for example a validation section for a screen with
   50+ fields × multiple validation types), split the section write into sub-batches:
   - Generate TCs for the first ~`settings.generation.validation_section_batch_size` items, append via Edit
   - Generate the next ~`settings.generation.validation_section_batch_size` items, append via Edit
   - Continue until the section is complete

   **Recovery on partial failure:**
   If an Edit tool call fails mid-section:
   - Read the current file to determine what was already written (last TC ID)
   - Resume generation from the next TC ID
   - Do NOT restart the entire section — existing content is valid
   - If the file is corrupted (truncated mid-TC), delete the last incomplete TC block and re-generate from that point

   **Small screen shortcut:** If the BD has ≤ `settings.generation.small_screen_item_threshold` items and no groups, the agent MAY write the entire file in one Write call. Use incremental strategy when output is expected to exceed ~`settings.generation.large_output_line_threshold` lines.

### Step 8: Generate Function Test Cases

   **BD content loading**: If using two-pass strategy (Step 4), read only the BD.md sections
   relevant to this TC category before generating. Use `line_range` from bd-summary.json
   to target reads. If not using two-pass, BD.md is already fully loaded.

   - Follow function-testcase-template.md as format scaffold only
   - Use the **resolved Function section list** from Step 6.5 (not hardcoded template sections).
   - Apply the same rules from Step 3
   - **Apply clarifications from Step 5** — same as Step 7.
   - Apply same group headings as Step 7 (if groups were confirmed)
   - **Function TCs use 4 fields:** Title, Precondition, Steps, Expected
   - Same Vietnamese content, config substitution
   - Use TC ID format supporting 2 or 3 levels:
     - 2-level: `FUNC_A.1_1` (for A.1 subsection)
     - 3-level: `FUNC_A.2.1_1`, `FUNC_A.3.4_2` (for nested sub-subsections like A.2.1, A.3.4)
     Always include `FUNC_` prefix, no zero-padding.

   **Write Strategy (incremental — for large output):**

   1. Generate TCs for section A header + subsection A.1 (Screen init) first.
   2. Write the file header + section A header + A.1 content using Write tool → creates `function-testcases.md`.
   3. For subsection A.2 (Function flow):
      a. Write A.2 subsection header.
      b. For each sub-subsection (A.2.1=Create, A.2.2=Update, ...):
         - Generate TCs for that sub-subsection.
         - Append to `function-testcases.md` using Edit tool.
   4. For subsection A.3 (Validation):
      a. Write A.3 subsection header.
      b. For each sub-subsection (A.3.1=Required, A.3.2=Maxlength, ...):
         - Generate TCs for that sub-subsection.
         - Append to `function-testcases.md` using Edit tool.
   5. After all subsections written, verify the complete file.

   **Large section handling:**
   If a single section is expected to produce > `settings.generation.large_section_tc_threshold` TCs (for example a validation section for a screen with
   50+ fields × multiple validation types), split the section write into sub-batches:
   - Generate TCs for the first ~`settings.generation.validation_section_batch_size` items, append via Edit
   - Generate the next ~`settings.generation.validation_section_batch_size` items, append via Edit
   - Continue until the section is complete

   **Recovery on partial failure:**
   If an Edit tool call fails mid-section:
   - Read the current file to determine what was already written (last TC ID)
   - Resume generation from the next TC ID
   - Do NOT restart the entire section — existing content is valid
   - If the file is corrupted (truncated mid-TC), delete the last incomplete TC block and re-generate from that point

   **Small screen shortcut:** If the BD has ≤ `settings.generation.small_screen_item_threshold` items and no groups, the agent MAY write the entire file in one Write call. Use incremental strategy when output is expected to exceed ~`settings.generation.large_output_line_threshold` lines.

### Step 9: Verify Output
   - Both files exist and non-empty
   - File structure is complete: starts with `# GUI Test Cases` / `# Function Test Cases` header,
     ends with the last section's TCs (no truncation mid-section)
   - All expected sections/subsections present (based on BD content — only component types/function areas that exist on the screen are required)
   - No duplicate section headers (would indicate append error)
   - **GUI TCs:** 4 fields per TC (Title, Precondition optional, Steps required, Expected). Steps describes the user action.
   - **Function TCs:** 4 fields per TC (Title, Precondition, Steps, Expected)
   - TC IDs follow convention:
     - GUI: `GUI_{letter}.{section}_{seq}` (e.g. `GUI_A.1_1`, `GUI_A.2_3`)
     - Function: `FUNC_{letter}.{section}[.{subsection}]_{seq}` (e.g. `FUNC_A.1_1`, `FUNC_A.2.1_1`, `FUNC_A.3.4_2`)
   - If grouped: each group has `## Group:` heading; if flat: no `## Group:` headings
   - Key-value format used throughout
   - Any `<!-- CLARIFICATION NEEDED: ... -->` comments are present only for genuinely unresolved items from Step 5

### Step 10: Print Summary
   ```
   Generated test cases for: {screen_name}
   GUI TCs: {count} | Function TCs: {count}

   When to run review first:
     ✅ YES — first time generating this screen
     ✅ YES — rules or BD have changed since last review
     ✅ YES — complex screen with many edge cases
     ⏭️  SKIP — small isolated change (1-2 TCs added), already manually verified
     ⏭️  SKIP — re-export with no content changes

   Next options:
     - /gen-testcase review {module}   (AI quality review, recommended)
     - /gen-testcase export {module}   (skip review, export directly)
   ```

   When invoked as part of `run` pipeline, the skill will ask the user via
   `Question Tool` whether to proceed with review or export directly. See
   SKILL.md "Run pipeline (with review gate)" for the full gate sequence.

## Rules file — single source of truth

Design rules are **not duplicated here**. They live in the rules file loaded in Step 3
(module override → global → `references/templates/rules-default.md`). To change how
TCs are designed, edit the rules file — never edit templates or this workflow.
