# Convert Workflow

Convert the configured BD source file to normalized `BD.md` (markdown) with extracted images when applicable.

## Arguments

- `{module}` — module name (matches directory under `docs/testcases/`)

## Steps

### Step 1: Read Config

   ```
   docs/testcases/{module}/config.yaml
   ```
   Get:
   - `bd_files` (ordered list of source files; may be empty)
   - `input_format` (default: `auto`)
   - `screen_name`, `screen_id`, `project`, `author`, `reviewer`

### Step 1.5: Validate Config

After reading config, validate critical fields before proceeding:

**Required (stop with error if missing):**
- `screen_name` — error: `"config.yaml: screen_name is empty. Run /gen-testcase init {module} or edit config.yaml."`
- `screen_id` — error: `"config.yaml: screen_id is empty. Run /gen-testcase init {module} or edit config.yaml."`

**Recommended (warn but continue):**
- `project` — warn: `"config.yaml: project is empty. Export filename will be incomplete."`
- `author`, `reviewer` — warn: `"config.yaml: author/reviewer empty. Export template placeholders won't be filled."`

**File existence:**
- If `bd_files` is set, check each path exists → stop with error listing all missing files

**YAML syntax:** If config.yaml failed to parse in Step 1, the workflow already stops. This step handles semantically valid but incomplete configs.

### Step 2: Resolve BD Source File(s)

Apply deterministic resolution rules in order:

1. **If `bd_files` is set and non-empty**:
   - Validate each path exists under `docs/testcases/{module}/`
   - If any file is missing → stop with error listing the missing file(s)
   - Resolved: list of source files in config order

2. **Else (bd_files is empty)**:
   - Scan `docs/testcases/{module}/input/` for supported candidates (`.xlsx`, `.md`), excluding `input/BD.md`
   - **Exactly 1 candidate** → use it, persist as `bd_files: [filename]` in config
   - **Multiple candidates** → use `Question Tool` to let user:
     - Pick one (persists as `bd_files: [filename]`)
     - OR select multiple and set merge order (persists as `bd_files`)
   - **0 candidates** → stop with error:
     ```
     No supported BD source file found in docs/testcases/{module}/input/.
     Supported formats: .xlsx, .md
     Add a BD source file or set `bd_files` explicitly, then re-run.
     ```

Resolved paths:
- Source file(s): `docs/testcases/{module}/{path}` for each entry
- Normalized output MD: `docs/testcases/{module}/input/BD.md` (merged)
- Output images: `docs/testcases/{module}/input/images/`

### Step 3: Install Dependencies

   ```bash
   pip install -r .agents/skills/gen-testcase/scripts/requirements.txt
   ```

   Skip if packages already installed (pip is idempotent).
   **Note for `run` pipeline**: If running as part of `run`, this install covers both convert and export. Export should skip its own install step.

### Step 4: Run Conversion / Normalization

   Based on the resolved file extension(s) (unless `input_format` explicitly pins a format):

   - `.xlsx` (single or multiple) → run convert script:
     ```bash
     python3 .agents/skills/gen-testcase/scripts/convert-bd-to-md.py \
       --input docs/testcases/{module}/{bd_file_1} \
       [--input docs/testcases/{module}/{bd_file_2}] \
       [--input docs/testcases/{module}/{bd_file_N}] \
       --output docs/testcases/{module}/input/BD.md \
       --images-dir docs/testcases/{module}/input/images/
     ```
     When multiple inputs are provided, the script merges them in argument order.
     Each input file's sheets are appended as separate sections in BD.md.

   - `.md` (single):
     - if configured source is exactly `input/BD.md` → no-op for content generation
     - if configured source is another `.md` file → copy/normalize it to `input/BD.md`
   - `.md` (multiple) — concatenate in order into `input/BD.md` with `---` separator
   - Mixed formats (`.xlsx` + `.md`) → convert each `.xlsx` to intermediate MD first, then concatenate all MDs in order
   - Any other extension → error:
     ```
     Format not yet supported. Convert to .xlsx or .md first.
     ```

   If `input_format` is not `auto` and does not match the resolved file extension → error with guidance to fix `config.yaml`.

### Step 5: Pull Enrichment

Optional enrichment step:
   - If `enrichment.screenshots` has configured paths:
     - copy or reference those image files into `docs/testcases/{module}/input/images/`
   - If enrichment references Figma or Jira:
     - treat them as future adapters
     - if required MCP/tooling is unavailable, print an info message and continue

### Step 6: Verify Output
   - `BD.md` exists and is non-empty
   - `images/` directory exists (it may be empty)
   - `BD.md` has readable markdown structure for downstream generation

### Step 6.5: Extract BD Summary (optional, for token optimization)

   ```bash
   python3 .agents/skills/gen-testcase/scripts/extract-bd-summary.py \
     --input docs/testcases/{module}/input/BD.md \
     --output docs/testcases/{module}/input/bd-summary.json
   ```

   This produces a structured summary for the generate workflow to use as a planning aid.
   If the script fails or produces warnings, continue — the summary is optional.

### Step 7: Print Summary
   ```
   Source: {bd_files} → BD.md
   Format: {resolved_format} | Images: {n} | Summary: {yes/no}

   Next: /gen-testcase generate {module}
   ```
