# Function Test Cases — {screen_name}

> Generated from: {bd_files}
> Screen ID: {screen_id}
> Mode: {mode}

> ⚠️ **SUB-SUBSECTION ASSIGNMENTS ARE CONDITIONAL, NOT PRE-FIXED.**
> Per `settings.yaml` `generation.function_flow_subsections` and `generation.validation_subsections`: only generate sub-subsections (`A.2.1`, `A.3.1`, ...) for functionality that actually exists on the screen.
> Example: a screen without Calendar field → omit `### FUNC A.3.6 Calendar` entirely. Do NOT leave empty placeholder subsections.

<!--
This file is a FORMAT SCAFFOLD only — section headers + example TC shape.
Generation rules live in the rules file loaded by generate-workflow.md Step 2
(module → global → rules-default.md). Do not duplicate rules here.

ID FORMAT: FUNC_{letter}.{section}[.{subsection}]_{seq}
  2-level: FUNC_A.1_1, FUNC_A.1_2
  3-level: FUNC_A.2.1_1, FUNC_A.3.4_2
Prefix FUNC_ is required. No zero-padding.
Subsections can nest up to 3 levels (e.g. A.2.1, A.3.8).

FIELDS (4 per TC):
  - Title (required), Precondition (required), Steps (required),
    Expected (required)

SECTION STRUCTURE:
Organized under a single top-level section A with 3 main subsections:
  A.1 — Screen initialization
  A.2 — Function flow (with sub-subsections A.2.1=Create, A.2.2=Update, etc.)
  A.3 — Validation (with sub-subsections A.3.1=Required, A.3.2=Maxlength, etc.)
Only generate sub-subsections for functionality that exists on the screen.

GROUP HEADING (optional — use only when BD has multiple CRUD modes / roles):
  ## Group: {code} — {label}
  Place above the first subsection of that group. TC IDs may repeat across groups.
  When no grouping is needed, omit entirely — parser injects an implicit N/A group automatically.
-->

<!-- Numbering below (A.2.1 Thêm mới, A.3.1 Required, ...) is illustrative only. Actual sub-subsection assignments come from settings.yaml `generation.function_flow_subsections` and `generation.validation_subsections`. Only generate sub-subsections for functionality that exists on the screen. -->

## A. {screen_name} screen

### FUNC A.1 Kiểm tra khởi tạo màn hình

> Pre-condition: {Describe full navigation path to reach screen}

### FUNC_A.1_1
- **Precondition:** Normal data
- **Title:** Kiểm tra khởi tạo màn hình với dữ liệu bình thường
- **Steps:** {navigation steps to open screen}
- **Expected:** {screen loads, fields default state, buttons enable/disable}

### FUNC_A.1_2
- **Precondition:** Maxlength data in all fields
- **Title:** Kiểm tra khởi tạo màn hình với dữ liệu maxlength
- **Steps:** {open record with max-length values}
- **Expected:** {no overflow/truncation, all data displayed correctly}

### FUNC A.2 Kiểm tra luồng chức năng

### FUNC A.2.1 Thêm mới

> Pre-condition: {navigation to create mode}

### FUNC_A.2.1_1
- **Precondition:** Đang ở màn hình thêm mới, chưa nhập dữ liệu
- **Title:** Kiểm tra hủy thêm mới (Cancel)
- **Steps:** Click nút Cancel/Hủy
- **Expected:** {quay lại màn hình trước, không lưu dữ liệu}

### FUNC_A.2.1_2
- **Precondition:** Đã nhập đầy đủ dữ liệu hợp lệ
- **Title:** Kiểm tra hiển thị popup xác nhận khi lưu
- **Steps:** Click nút Save/Lưu
- **Expected:** {hiển thị popup xác nhận lưu}

### FUNC_A.2.1_3
- **Precondition:** Popup xác nhận đang hiển thị
- **Title:** Kiểm tra hủy tại popup xác nhận
- **Steps:** Click Cancel trên popup xác nhận
- **Expected:** {đóng popup, quay lại màn hình thêm mới, dữ liệu không thay đổi}

### FUNC_A.2.1_4
- **Precondition:** Popup xác nhận đang hiển thị
- **Title:** Kiểm tra xác nhận thêm mới thành công
- **Steps:** Click OK trên popup xác nhận
- **Expected:** {thêm mới thành công, hiển thị message thành công}

### FUNC_A.2.1_5
- **Precondition:** Đã thêm mới thành công
- **Title:** Kiểm tra dữ liệu DB sau khi thêm mới
- **Steps:** Query DB kiểm tra dữ liệu
- **Expected:** {dữ liệu trong DB khớp với input}

### FUNC_A.2.1_6
- **Precondition:** Đã thêm mới thành công
- **Title:** Kiểm tra log sau khi thêm mới
- **Steps:** Kiểm tra log table
- **Expected:** {log ghi nhận đúng action, user, timestamp}

### FUNC A.2.2 Cập nhật

> Pre-condition: {navigation to edit mode with existing data}

### FUNC_A.2.2_1
- **Precondition:** Mở màn hình cập nhật, không thay đổi dữ liệu
- **Title:** Kiểm tra cập nhật khi không thay đổi dữ liệu
- **Steps:** Click nút Save/Lưu mà không thay đổi gì
- **Expected:** {behavior khi không có thay đổi}

### FUNC_A.2.2_2
- **Precondition:** Đã thay đổi dữ liệu hợp lệ
- **Title:** Kiểm tra hiển thị popup xác nhận cập nhật
- **Steps:** Thay đổi dữ liệu, click Save
- **Expected:** {hiển thị popup xác nhận cập nhật}

### FUNC_A.2.2_3
- **Precondition:** Popup xác nhận đang hiển thị
- **Title:** Kiểm tra hủy cập nhật tại popup xác nhận
- **Steps:** Click Cancel trên popup
- **Expected:** {đóng popup, dữ liệu không thay đổi trong DB}

### FUNC_A.2.2_4
- **Precondition:** Popup xác nhận đang hiển thị
- **Title:** Kiểm tra cập nhật thành công
- **Steps:** Click OK trên popup xác nhận
- **Expected:** {cập nhật thành công, hiển thị message}

### FUNC_A.2.2_5
- **Precondition:** Đã cập nhật thành công
- **Title:** Kiểm tra dữ liệu DB sau cập nhật
- **Steps:** Query DB kiểm tra dữ liệu
- **Expected:** {dữ liệu trong DB khớp với dữ liệu đã thay đổi}

### FUNC A.3 Kiểm tra validate

### FUNC A.3.1 Required

> Pre-condition: {navigation to input form}

### FUNC_A.3.1_1
- **Precondition:** Field {field_name} để trống
- **Title:** Kiểm tra validate required {field_name}
- **Steps:** Để trống {field_name}, click Save
- **Expected:** Hiển thị error message: {error_message}

### FUNC A.3.2 Maxlength

### FUNC_A.3.2_1
- **Precondition:** Field {field_name} nhập vượt quá {maxlength} ký tự
- **Title:** Kiểm tra validate maxlength {field_name}
- **Steps:** Nhập {maxlength + 1} ký tự vào {field_name}
- **Expected:** {không cho nhập quá maxlength hoặc hiển thị error}

### FUNC A.3.3 Text (free text)

### FUNC_A.3.3_1
- **Precondition:** Field {field_name} là text tự do
- **Title:** Kiểm tra nhập ký tự đặc biệt vào {field_name}
- **Steps:** Nhập ký tự đặc biệt (!@#$%^&*), click Save
- **Expected:** {lưu thành công hoặc hiển thị error tùy theo spec}

### FUNC A.3.4 Listbox

### FUNC_A.3.4_1
- **Precondition:** Field {field_name} là listbox
- **Title:** Kiểm tra chọn giá trị từ listbox {field_name}
- **Steps:** Chọn một giá trị từ listbox, click Save
- **Expected:** {lưu đúng giá trị đã chọn}

### FUNC A.3.5 Checkbox

### FUNC_A.3.5_1
- **Precondition:** Field {field_name} là checkbox
- **Title:** Kiểm tra toggle checkbox {field_name}
- **Steps:** Check/Uncheck {field_name}, click Save
- **Expected:** {lưu đúng trạng thái checkbox}

### FUNC A.3.6 Calendar

### FUNC_A.3.6_1
- **Precondition:** Field {field_name} là calendar
- **Title:** Kiểm tra chọn ngày từ calendar {field_name}
- **Steps:** Chọn ngày từ calendar picker, click Save
- **Expected:** {hiển thị đúng format, lưu đúng giá trị}

### FUNC A.3.7 Timepicker

### FUNC_A.3.7_1
- **Precondition:** Field {field_name} là timepicker
- **Title:** Kiểm tra chọn thời gian từ timepicker {field_name}
- **Steps:** Chọn thời gian, click Save
- **Expected:** {hiển thị đúng format, lưu đúng giá trị}

### FUNC A.3.8 Start/End time relationship

### FUNC_A.3.8_1
- **Precondition:** Cả hai field {start_field} và {end_field} đã nhập
- **Title:** Kiểm tra validate thời gian bắt đầu > kết thúc
- **Steps:** Nhập {start_field} > {end_field}, click Save
- **Expected:** Hiển thị error message: {error_message}
