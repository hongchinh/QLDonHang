# Export Workflow

Export markdown test cases to xlsx using the DJP template and a self-contained Python script.

## Arguments

- `{module}` — module name (matches directory under `docs/testcases/`)

## Steps

### Step 1: Resolve Paths
   - Config: `docs/testcases/{module}/config.yaml`
   - GUI MD: `docs/testcases/{module}/output/gui-testcases.md`
   - Function MD: `docs/testcases/{module}/output/function-testcases.md`
   - Template: `.agents/skills/gen-testcase/references/templates/DJP_TestCase_Template_Ver1.0.xlsx`
   - Output: `docs/testcases/{module}/output/{project_name}_{screen_name}_Testcase_v{version}.xlsx`
     (version may be updated by Step 1.5 auto-increment before this path is finalized)

### Step 1.5: Version Check & Auto-Increment

   Check for existing output files in `docs/testcases/{module}/output/`:

   1. Scan for files matching pattern: `{project_name}_{screen_name}_Testcase_v*.xlsx`
   2. If no existing files found → use `version` from config as-is, continue to Step 2
   3. If existing file(s) found:
      a. Extract the highest version number from existing filenames
      b. Compute next version: increment minor by 1 (e.g. `1.0` → `1.1`, `2.3` → `2.4`)
      c. Use `Question Tool` to present options:

         Header: "Version"
         Question: "Found existing export v{current}. Choose version for this export:"
         Options:
         - "v{next} (Recommended)" — auto-incremented version
         - "v{current} — overwrite existing" — keep same version, overwrite
         - "Custom version" — let user type a version string

      d. If user picks auto-increment or custom:
         - Update `version` field in `docs/testcases/{module}/config.yaml`
         - Use the new version for the output filename
      e. If user picks overwrite:
         - Keep existing version, file will be overwritten

   **Version format**: supports `X.Y` (major.minor) strings. Minor is incremented.
   For non-standard version strings (e.g. `1.0-beta`), present options but default to appending `.1`.

### Step 2: Verify Prerequisites
   - `config.yaml` exists
   - `gui-testcases.md` exists and is non-empty
   - `function-testcases.md` exists and is non-empty
   - Template xlsx exists
     - If missing, error:
       ```
       Template file not found.
       Place DJP_TestCase_Template_Ver1.0.xlsx in references/templates/
       ```

### Step 3: Install Dependencies

   ```bash
   pip install -r .agents/skills/gen-testcase/scripts/requirements.txt
   ```

   Skip if packages already installed (pip is idempotent).
   **Skip if `convert` was already run in this session** — dependencies are the same.

### Step 4: Run Script

   ```bash
   python3 .agents/skills/gen-testcase/scripts/export-tc-to-xlsx.py \
     --config docs/testcases/{module}/config.yaml \
     --template .agents/skills/gen-testcase/references/templates/DJP_TestCase_Template_Ver1.0.xlsx \
     --gui docs/testcases/{module}/output/gui-testcases.md \
     --function docs/testcases/{module}/output/function-testcases.md \
     --output docs/testcases/{module}/output/{project_name}_{screen_name}_Testcase_v{version}.xlsx
   ```

   The script:
   - Loads the template workbook, preserving all formatting, sheet structure, and formulas
   - Replaces `{placeholders}` with values from config.yaml
   - Parses GUI and Function markdown files into ordered TC entries
   - Fills the `GUI` and `FUNC` sheets with parsed entries
   - Saves the output workbook

### Step 5: Verify Output
   - xlsx file exists at the resolved output path
   - Has required sheets: Cover, Changed History, Test Report, Common, GUI, FUNC, Screen Layout
   - Open the file to confirm data is visible in GUI and FUNC sheets

### Step 6: Print Summary
   ```
   Exported: {output_filename}
   Sheets: {n} | GUI TCs: {n} | FUNC TCs: {n} | Common TCs: {n}
   Location: docs/testcases/{module}/output/
   ```
