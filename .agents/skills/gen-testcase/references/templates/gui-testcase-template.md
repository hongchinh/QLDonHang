# GUI Test Cases — {screen_name}

> Generated from: {bd_files}
> Screen ID: {screen_id}
> Mode: {mode}

> ⚠️ **SUBSECTION NUMBERING IS SEQUENTIAL, NOT FIXED.**
> Per `generate-workflow.md` Step 6.5: assign subsection numbers SEQUENTIALLY (A.1, A.2, ...) based on which component types actually exist on the screen.
> Example: a read-only screen with only Label + Button → `### GUI A.1 Label`, `### GUI A.2 Button` (NOT A.1, A.5).

<!--
This file is a FORMAT SCAFFOLD only — section headers + example TC shape.
Generation rules live in the rules file loaded by generate-workflow.md Step 2
(module → global → rules-default.md). Do not duplicate rules here.

ID FORMAT: GUI_{letter}.{section}_{seq}  (e.g. GUI_A.1_1, GUI_A.2_3, GUI_A.8_1)
Prefix GUI_ is required. No zero-padding.

FIELDS (4 per TC):
  - Title (required): Vietnamese description of what is being tested
  - Precondition (optional): state before test — omit line if not needed
  - Steps (required): terse Vietnamese description of the user action being verified
  - Expected (required): expected visible result

SECTION STRUCTURE:
Organized by UI component type under a single top-level section A.
Subsections are numbered sequentially (A.1, A.2, ...) based on which
component types exist on the screen. Do NOT use fixed A.1–A.8 mapping.
Component types: Label, Textbox, Dropdown, Radio, Checkbox, Button,
Link, Text area, Calendar, Pagination.
Only generate subsections for component types that exist on the screen.

GROUP HEADING (optional — use only when BD has multiple CRUD modes / roles):
  ## Group: {code} — {label}
  Place above the first subsection of that group. TC IDs may repeat across groups.
  When no grouping is needed, omit entirely — parser injects an implicit N/A group automatically.
-->

<!-- Numbering below (A.1 Textbox, A.2 Dropdown, ...) is illustrative only. Actual subsection numbers are assigned sequentially per generate-workflow.md Step 6.5 based on which component types exist in the BD. -->

## A. {screen_name} screen

### GUI A.1 Textbox

> Pre-condition: {Describe navigation steps to reach this screen}

### GUI_A.1_1
- **Title:** Kiểm tra tên field {field_name} mặc định
- **Steps:** Quan sát label của field {field_name} trên màn hình
- **Expected:** Hiển thị tên: {field_name}

### GUI_A.1_2
- **Title:** Kiểm tra tên field {field_name} khi chuyển đổi ngôn ngữ
- **Precondition:** Chuyển đổi ngôn ngữ sang {language}
- **Steps:** Quan sát label của field {field_name} sau khi đổi ngôn ngữ
- **Expected:** Hiển thị tên: {field_name_translated}

### GUI_A.1_3
- **Title:** Kiểm tra dấu (*) required của {field_name}
- **Steps:** Quan sát biểu tượng (*) bên cạnh tên field {field_name}
- **Expected:** Hiển thị dấu (*) bên cạnh tên field

### GUI_A.1_4
- **Title:** Kiểm tra format hiển thị {field_name}
- **Steps:** Nhập dữ liệu vào {field_name} và quan sát format hiển thị
- **Expected:** {expected format — e.g., date format, number format}

### GUI_A.1_5
- **Title:** Kiểm tra placeholder của {field_name}
- **Steps:** Quan sát placeholder của {field_name} khi chưa nhập dữ liệu
- **Expected:** Hiển thị placeholder: {placeholder_text}

### GUI_A.1_6
- **Title:** Kiểm tra maxlength của {field_name}
- **Precondition:** Nhập dữ liệu vượt quá {maxlength} ký tự
- **Steps:** Nhập liên tục cho đến khi vượt {maxlength} ký tự vào {field_name}
- **Expected:** Không cho nhập quá {maxlength} ký tự

### GUI_A.1_7
- **Title:** Kiểm tra giá trị min/max của {field_name}
- **Steps:** Nhập giá trị min và max vào {field_name}
- **Expected:** {expected min/max behavior}

### GUI_A.1_8
- **Title:** Kiểm tra max data length hiển thị của {field_name}
- **Precondition:** Dữ liệu {field_name} có độ dài tối đa
- **Steps:** Quan sát hiển thị của {field_name} với dữ liệu max length
- **Expected:** Hiển thị đầy đủ, không bị tràn hoặc cắt

### GUI A.2 Dropdown (Listbox)

### GUI_A.2_1
- **Title:** Kiểm tra tên field {field_name} mặc định
- **Steps:** Quan sát label của dropdown {field_name} trên màn hình
- **Expected:** Hiển thị tên: {field_name}

### GUI_A.2_2
- **Title:** Kiểm tra tên field {field_name} khi chuyển đổi ngôn ngữ
- **Precondition:** Chuyển đổi ngôn ngữ sang {language}
- **Steps:** Quan sát label của dropdown {field_name} sau khi đổi ngôn ngữ
- **Expected:** Hiển thị tên: {field_name_translated}

### GUI_A.2_3
- **Title:** Kiểm tra giá trị mặc định của {field_name}
- **Steps:** Quan sát giá trị mặc định của dropdown {field_name}
- **Expected:** Hiển thị giá trị mặc định: {default_value}

### GUI_A.2_4
- **Title:** Kiểm tra dấu (*) required của {field_name}
- **Steps:** Quan sát biểu tượng (*) bên cạnh tên field {field_name}
- **Expected:** Hiển thị dấu (*) bên cạnh tên field

### GUI_A.2_5
- **Title:** Kiểm tra danh sách dữ liệu của {field_name}
- **Steps:** Mở dropdown {field_name} và quan sát danh sách options
- **Expected:** Hiển thị danh sách: {data_list}

### GUI_A.2_6
- **Title:** Kiểm tra placeholder của {field_name}
- **Steps:** Quan sát placeholder của {field_name} khi chưa chọn giá trị
- **Expected:** Hiển thị placeholder: {placeholder_text}

### GUI_A.2_7
- **Title:** Kiểm tra max data length hiển thị của {field_name}
- **Precondition:** Dữ liệu {field_name} có độ dài tối đa
- **Steps:** Mở dropdown {field_name} và quan sát hiển thị giá trị
- **Expected:** Hiển thị đầy đủ, không bị tràn

### GUI A.3 Radio button

### GUI_A.3_1
- **Title:** Kiểm tra tên field {field_name} mặc định
- **Steps:** Quan sát label của radio group {field_name} trên màn hình
- **Expected:** Hiển thị tên: {field_name}

### GUI_A.3_2
- **Title:** Kiểm tra giá trị mặc định được chọn của {field_name}
- **Steps:** Quan sát option đang được chọn mặc định của {field_name}
- **Expected:** Giá trị mặc định: {default_value}

### GUI_A.3_3
- **Title:** Kiểm tra danh sách options của {field_name}
- **Steps:** Quan sát các option của radio group {field_name}
- **Expected:** Hiển thị options: {options_list}

### GUI A.4 Checkbox

### GUI_A.4_1
- **Title:** Kiểm tra tên field {field_name} mặc định
- **Steps:** Quan sát label của checkbox {field_name} trên màn hình
- **Expected:** Hiển thị tên: {field_name}

### GUI_A.4_2
- **Title:** Kiểm tra trạng thái mặc định của {field_name}
- **Steps:** Quan sát trạng thái checked/unchecked mặc định của {field_name}
- **Expected:** Checkbox mặc định: {checked/unchecked}

### GUI A.5 Button

### GUI_A.5_1
- **Title:** Kiểm tra tên button {button_name} mặc định
- **Steps:** Quan sát label của button {button_name} trên màn hình
- **Expected:** Hiển thị tên: {button_name}

### GUI_A.5_2
- **Title:** Kiểm tra trạng thái enable/disable của {button_name}
- **Precondition:** {condition for enable/disable}
- **Steps:** Quan sát trạng thái enable/disable của button {button_name}
- **Expected:** Button {enabled/disabled}

### GUI A.6 Link

### GUI_A.6_1
- **Title:** Kiểm tra tên link {link_name} mặc định
- **Steps:** Quan sát label của link {link_name} trên màn hình
- **Expected:** Hiển thị tên: {link_name}

### GUI_A.6_2
- **Title:** Kiểm tra điều hướng khi click {link_name}
- **Steps:** Click vào link {link_name}
- **Expected:** Điều hướng đến: {destination}

### GUI A.7 Text area

### GUI_A.7_1
- **Title:** Kiểm tra tên field {field_name} mặc định
- **Steps:** Quan sát label của text area {field_name} trên màn hình
- **Expected:** Hiển thị tên: {field_name}

### GUI_A.7_2
- **Title:** Kiểm tra maxlength của {field_name}
- **Precondition:** Nhập dữ liệu vượt quá {maxlength} ký tự
- **Steps:** Nhập liên tục cho đến khi vượt {maxlength} ký tự vào {field_name}
- **Expected:** Không cho nhập quá {maxlength} ký tự

### GUI A.8 Calendar (Timepicker)

### GUI_A.8_1
- **Title:** Kiểm tra tên field {field_name} mặc định
- **Steps:** Quan sát label của field {field_name} trên màn hình
- **Expected:** Hiển thị tên: {field_name}

### GUI_A.8_2
- **Title:** Kiểm tra format hiển thị của {field_name}
- **Steps:** Mở calendar picker của {field_name} và quan sát format
- **Expected:** Hiển thị format: {date_format}

### GUI_A.8_3
- **Title:** Kiểm tra giá trị mặc định của {field_name}
- **Steps:** Quan sát giá trị mặc định của {field_name}
- **Expected:** Giá trị mặc định: {default_value}
