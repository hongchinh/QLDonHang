# Init Workflow

Initialize a test case module directory for a specific screen/feature.

## Arguments

- `{module}` — kebab-case name (e.g., `create-treatment-report`)

## Steps

### Step 1: Create Directory Structure
   ```
   docs/testcases/{module}/
   ├── input/
   │   ├── images/
   │   └── context/
   ├── output/
   └── config.yaml
   ```

### Step 2: Generate Config

Generate config.yaml from template `references/templates/config-template.yaml`:
   - Copy template to `docs/testcases/{module}/config.yaml`
   - Ask user to fill: `screen_name`, `screen_id`, `project_name`, `author`, `reviewer`
   - Auto-fill `created_date` with today's date

### Step 3: Ask About Groups

Use `Question Tool` to ask:
> "Does this screen have multiple CRUD modes, roles, or service variants? (e.g. A2 Create vs R2 Edit)"

   Options: **Yes** | **No**

   - If **Yes**: ask user to list the group codes and labels (e.g. `A2: 新規 Create Patient, R2: 修正 Edit Patient`). Write `group_type` and `groups` list into `config.yaml`.
   - If **No**: leave `group_type: none` and `groups: []` as-is (defaults).

### Step 4: Ask About Enrichment Sources

Use `Question Tool` to ask:
> "Do you have additional reference sources? (screenshots, Figma, Jira)"

   Options: **No, just BD file** | **Yes, I'll add them later**

   - If **Yes**: keep the `enrichment` section in `config.yaml` and tell the user to populate it later as needed.
   - If **No**: leave the default empty `enrichment` values as-is.

### Step 5: Setup Rules File

   - Check if `docs/testcases/{module}/input/rules.md` exists → skip
   - Check if `docs/testcases/rules.md` exists → skip (global rules available)
   - Otherwise, copy `references/templates/rules-default.md` → `docs/testcases/rules.md`

### Step 6: Print Instructions
   ```
   Module initialized: docs/testcases/{module}/

   Next steps:
   1. Place BD source file(s) in docs/testcases/{module}/input/
      - Supported: .xlsx, .md
      - Single source: set `bd_files: ["input/filename.xlsx"]` in config.yaml
      - Multiple sources: set `bd_files` list in config.yaml (merged in order)
      - If `bd_files` is empty, `convert` auto-detects candidates in `input/`
      - If multiple candidates exist, `convert` asks you to choose and then persists `bd_files`
   2. Edit config.yaml with screen metadata and `bd_files` as needed
   3. (Optional) Add screenshots to input/images/ and enrichment notes to input/context/
   4. (Optional) Add custom rules to input/rules.md (module) or docs/testcases/rules.md (project-wide)
   5. Ensure the export template exists at .agents/skills/gen-testcase/references/templates/DJP_TestCase_Template_Ver1.0.xlsx
   6. Run: /gen-testcase convert {module}
   ```
