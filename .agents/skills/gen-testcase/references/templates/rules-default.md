# Rules for Test Case Generation

Các quy tắc bắt buộc khi tạo test case từ BD.

## Format Guide

Each rule must follow this format for the review workflow to parse and check compliance:

    N. **Rule name**: Description text here.

Where:
- `N` = sequential number (1, 2, 3...)
- `**Rule name**` = bold short name
- `:` separator is required
- Description follows on the same line or subsequent indented lines

Rules using other formats (bullet lists, plain text, headings) will NOT be detected
by the automated review step. When adding custom rules, follow this format strictly.

## Quy tắc chung

1. **Title format**: Viết title theo tiêu chí what-how-where, viết liền trong 1 câu ngắn gọn đủ ý.

2. **Per-field + combined**: Viết test case cho từng field, và test case kết hợp các field.

3. **Merge same-column TCs**: Nếu có 2 test case cho riêng lẻ 1 field thì gộp thành 1 test case (field ở đây là dữ liệu cùng column thì mới gộp).

4. **Layout per screen**: Nếu test case cho layout màn hình, tách riêng từng màn hình thành test case riêng.

5. **Sort per column**: Viết test case cho sort theo từng column, test case cho sort theo nhiều column cùng nhau.

6. **Merge sort TCs**: Nếu có 2 test case sort cho 1 column thì gộp thành 1 test case.

7. **Validation đầy đủ**: Các trường hợp validate cần viết cụ thể, đủ trường hợp validate.

8. **Free text minimal**: Trong ô nhập dữ liệu free text, chỉ cần 1 test case cơ bản, tester tự check các case normal ở test case này.

9. **Button validation**: Khi thực hiện action ở các button cần validate thì hãy viết test case cho các validate này.

10. **No SQL injection**: Không cần test case SQL injection.

11. **DB data generic**: Khi cần nhập thông tin tìm kiếm trong DB, hay so sánh dữ liệu hiển thị trong DB, không hard code data luôn (vì không đúng). Hãy ghi chung chung nhập dữ liệu có trong DB (bảng, cột cụ thể nếu có thông tin), so sánh dữ liệu hiển thị giống data trong DB không. Nếu dữ liệu cần format lại (khác text trong DB) cũng cần ghi rõ ra. Nếu có nhiều column tách test case kiểm tra dữ liệu cho từng column.

12. **Navigation per screen**: Nếu test case mà điều hướng màn hình, như click button, click menu, ... để mở màn hình khác, với mỗi màn hình khác được mở ra hãy viết một test case riêng.

13. **Multi-field validation grouped**: Nếu có nhiều field input cần validate, nhưng action đều phải click vào một button thì hãy gom chung các xử lý validate về validate của button.

14. **UI components separate**: Nếu có từ 2 test case cho hoạt động của checkbox, radio button, calendar, combobox, dropdown, button. Hãy tách riêng các test case, không áp dụng rule số 3.

15. **Bold section headers**: Cho bold vào các phần header của bảng tương ứng.

16. **Group heading convention**: Khi BD có nhiều CRUD mode / role / service variant trên cùng một màn hình (ví dụ: `A2` Create vs `R2` Edit), sử dụng `## Group: {code} — {label}` (H2) để phân nhóm. Mỗi group chứa các subsection và TC của nó. Khi không cần phân nhóm, bỏ qua heading này (parser sẽ tự xử lý).

    Ví dụ:
    ```markdown
    ## Group: A2 — 新規 Create Patient

    ### A.1 Textbox
    ### GUI_A.1_1

    ## Group: R2 — 修正 Edit Patient

    ### A.1 Textbox
    ### GUI_A.1_1
    - **Title:** …
    ```

17. **Expected Results format**: Viết Expected Results ở dạng multi-line với cấu trúc:
    - Dòng đầu: câu tóm tắt hành vi (ví dụ "Xác nhận tên các mục hiển thị trên màn hình").
    - Các dòng tiếp theo: leading `- ` hoặc ` - ` (dấu gạch đầu dòng) liệt kê từng điều kiện cần verify.
    - Nếu cần reference thiết kế/BD, dùng format `(Tham chiếu: {tên mục trong BD/Screen Layout})` hoặc `Tham chiếu: ...` ở dòng cuối.
    - Với message error cụ thể, quote nguyên văn từ BD bằng 「...」 hoặc `"..."`.

    Ví dụ đúng:
    ```
    Xác nhận tên các mục hiển thị trên màn hình
     - Tên các mục hiển thị đầy đủ thông tin như mô tả trong bản thiết kế màn hình
     - Không có văn bản sai hoặc chồng lắp nhau trong các mục trên màn hình
    (Tham chiếu: Screen Layout sheet)
    ```

18. **GUI field scope**: GUI test cases sử dụng 4 trường: Title, Precondition (optional), Steps (required), Expected. GUI Steps mô tả thao tác người dùng đang được kiểm tra, viết bằng tiếng Việt, súc tích (≤ 1 câu ngắn).

19. Câu từ cần rõ ràng, không gây khó hiểu khi đọc. Cần từ ngữ súc tích, không dài dòng, lan man.

20. Không gộp các testcase lại, tách rõ ràng theo từng điều kiện, luồng xử lý sao cho hợp lý

21. Phải phù hợp với thực tế execute của Tester, không viết những test case quá lý thuyết, không thực tế, không có khả năng execute được.
