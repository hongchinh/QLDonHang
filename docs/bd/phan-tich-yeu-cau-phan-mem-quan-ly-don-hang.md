# PHÂN TÍCH YÊU CẦU PHẦN MỀM QUẢN LÝ ĐƠN HÀNG

## 1. Thông tin chung

### 1.1. Tên tài liệu

Phân tích yêu cầu phần mềm quản lý đơn hàng, báo giá, in chứng từ và tổng hợp báo cáo.

### 1.2. Mục tiêu tài liệu

Tài liệu này tổng hợp yêu cầu nghiệp vụ cho phần mềm quản lý đơn hàng dựa trên các file Excel và ảnh chứng từ thực tế, bao gồm:

- Lập báo giá.
- In báo giá.
- Lập đơn hàng.
- In đơn hàng.
- In biên bản bàn giao.
- In phiếu xuất kho.
- Tổng hợp báo cáo doanh thu, lợi nhuận, công nợ, giao hàng.

### 1.3. Phạm vi áp dụng

Phần mềm áp dụng cho doanh nghiệp kinh doanh các nhóm hàng:

- Tấm xốp EPS.
- Tấm xốp XPS.
- Tấm xốp PE Foam.
- Thùng xốp.
- Da gel.
- Cao su non.
- Bông khoáng, bông thủy tinh.
- Hàng hóa, vật tư phụ trợ và dịch vụ vận chuyển liên quan.

---

## 2. Hiện trạng nghiệp vụ

Hiện tại quy trình bán hàng đang được xử lý bằng Excel và chứng từ in thủ công.

Các tài liệu/chứng từ hiện có gồm:

1. File theo dõi bán hàng theo tháng.
2. File báo giá thùng xốp.
3. File báo giá xác định lợi nhuận.
4. Mẫu bảng báo giá hàng hóa.
5. Mẫu bảng báo giá kiêm xác nhận đơn hàng.
6. Mẫu biên bản bàn giao hàng hóa.
7. Mẫu biên bản bàn giao hàng kiêm phiếu xuất kho.

### 2.1. Các thông tin xuất hiện trên chứng từ thực tế

Thông tin công ty bán:

- Tên công ty.
- Logo.
- Địa chỉ.
- Mã số thuế.
- Điện thoại.
- Email.
- Website.
- Danh sách nhóm sản phẩm công ty cung cấp.

Thông tin khách hàng:

- Đơn vị mua hàng.
- Mã số thuế.
- Địa chỉ.
- Người đặt hàng.
- Điện thoại người đặt.
- Người nhận hàng.
- Điện thoại người nhận.
- Địa chỉ nhận hàng.
- Thời gian nhận hàng.

Thông tin hàng hóa:

- STT.
- Nội dung hàng hóa.
- Đơn vị tính.
- Số lượng.
- Đơn giá.
- Thành tiền.
- Ghi chú.
- Kích thước/quy cách.
- Tỷ trọng.
- Số lượng m².
- Số lượng tấm.

Thông tin tài chính:

- Cộng tiền hàng.
- Thuế GTGT.
- Tổng cộng.
- Đã tạm ứng.
- Còn thanh toán.
- Cước vận chuyển.
- Giá nhập.
- Thành tiền nhập.
- Lợi nhuận/chênh lệch.

Thông tin chứng từ:

- Số báo giá.
- Ngày báo giá.
- Số đơn hàng.
- Ngày đơn hàng.
- Số biên bản bàn giao.
- Ngày bàn giao.
- Khu vực chữ ký bên mua/bên bán.
- QR chuyển khoản.
- Thông tin tài khoản ngân hàng.

### 2.2. Vấn đề hiện tại

- Dữ liệu bị phân tán ở nhiều file Excel.
- Dễ nhập sai số lượng, đơn giá, thuế, tổng tiền.
- Cùng một dữ liệu phải nhập lại nhiều lần giữa báo giá, đơn hàng và biên bản bàn giao.
- Khó theo dõi trạng thái báo giá, đơn hàng, giao hàng, thanh toán.
- Khó tổng hợp doanh thu, lợi nhuận, công nợ theo thời gian.
- Không kiểm soát được lịch sử chỉnh sửa chứng từ.
- Không phân quyền được người dùng xem giá vốn, lợi nhuận.
- Mẫu in phụ thuộc nhiều vào thao tác thủ công.
- Dữ liệu Excel có thể thiếu chuẩn hóa, có dòng trống kế thừa thông tin từ dòng trên.

### 2.3. Mục tiêu xây dựng phần mềm

Phần mềm cần thay thế quy trình Excel hiện tại, giúp quản lý đầy đủ luồng nghiệp vụ:

```text
Báo giá → Xác nhận đơn hàng → Lập đơn hàng → In chứng từ → Bàn giao hàng → Thanh toán → Báo cáo
```

Mục tiêu chính:

- Lập báo giá nhanh, chính xác.
- Tự động tính thành tiền, thuế, tổng cộng, công nợ, lợi nhuận.
- Chuyển báo giá thành đơn hàng mà không cần nhập lại.
- In báo giá, đơn hàng, biên bản bàn giao, phiếu xuất kho đúng mẫu.
- Quản lý trạng thái xử lý đơn hàng.
- Quản lý thanh toán, tạm ứng, còn phải thu.
- Tổng hợp báo cáo doanh thu, lợi nhuận, công nợ, giao hàng.
- Hạn chế sai sót trong quá trình nhập liệu.

---

## 3. Đối tượng sử dụng và phân quyền

### 3.1. Admin

Quản trị toàn bộ hệ thống.

Quyền chính:

- Quản lý người dùng.
- Quản lý vai trò và phân quyền.
- Cấu hình thông tin công ty.
- Cấu hình mẫu in.
- Cấu hình tài khoản ngân hàng, QR thanh toán.
- Quản lý danh mục khách hàng, hàng hóa.
- Xem toàn bộ báo cáo.
- Xem doanh thu, giá vốn, lợi nhuận.

### 3.2. Nhân viên kinh doanh

Phụ trách lập báo giá, đơn hàng.

Quyền chính:

- Lập báo giá.
- Sửa báo giá khi chưa khóa.
- In báo giá.
- Chuyển báo giá thành đơn hàng.
- Lập đơn hàng.
- Theo dõi đơn hàng của mình.
- Không được xem giá vốn/lợi nhuận nếu không được cấp quyền.

### 3.3. Kế toán

Phụ trách thanh toán, công nợ, doanh thu.

Quyền chính:

- Xem danh sách đơn hàng.
- Cập nhật tạm ứng.
- Cập nhật thanh toán.
- Theo dõi công nợ.
- Xem báo cáo doanh thu, công nợ.
- Xuất báo cáo Excel.

### 3.4. Kho / giao hàng

Phụ trách xử lý giao hàng và biên bản bàn giao.

Quyền chính:

- Xem đơn hàng chờ giao.
- In biên bản bàn giao.
- In phiếu xuất kho.
- Cập nhật trạng thái đã giao.
- Không được xem giá vốn/lợi nhuận nếu không được cấp quyền.

### 3.5. Quản lý

Phụ trách kiểm soát hoạt động kinh doanh.

Quyền chính:

- Xem toàn bộ báo giá, đơn hàng.
- Duyệt báo giá nếu cần.
- Xem báo cáo doanh thu.
- Xem báo cáo lợi nhuận.
- Xem công nợ.
- Xem hiệu quả kinh doanh theo khách hàng, sản phẩm, nhân viên.

---

## 4. Danh mục dữ liệu

## 4.1. Danh mục khách hàng

### 4.1.1. Mục đích

Quản lý thông tin khách hàng để sử dụng khi lập báo giá, đơn hàng, chứng từ bàn giao.

### 4.1.2. Trường dữ liệu

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Mã khách hàng | Có | Tự sinh hoặc nhập tay |
| Tên khách hàng | Có | Tên cá nhân hoặc công ty |
| Mã số thuế | Không | MST khách hàng nếu có |
| Địa chỉ công ty | Không | Địa chỉ pháp lý |
| Địa chỉ giao hàng mặc định | Không | Địa chỉ nhận hàng thường dùng |
| Người liên hệ | Không | Người đặt hàng |
| Số điện thoại | Không | SĐT người đặt/người nhận |
| Email | Không | Email khách hàng |
| Nhóm khách hàng | Không | Công ty, đại lý, khách lẻ, công trình |
| Ghi chú | Không | Ghi chú nội bộ |
| Trạng thái | Có | Đang sử dụng / Ngừng sử dụng |

### 4.1.3. Yêu cầu nghiệp vụ

- Một khách hàng có thể có nhiều địa chỉ giao hàng.
- Khi chọn khách hàng ở báo giá/đơn hàng, hệ thống tự điền:
  - Tên khách hàng.
  - MST.
  - Địa chỉ.
  - Người liên hệ.
  - Số điện thoại.
- Cho phép thêm nhanh khách hàng khi đang lập báo giá hoặc đơn hàng.
- Cho phép tìm kiếm khách hàng theo:
  - Tên khách hàng.
  - Số điện thoại.
  - Mã số thuế.
  - Địa chỉ giao hàng.

---

## 4.2. Danh mục địa chỉ giao hàng

### 4.2.1. Mục đích

Quản lý nhiều địa chỉ nhận hàng của một khách hàng.

### 4.2.2. Trường dữ liệu

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Khách hàng | Có | Khách hàng sở hữu địa chỉ |
| Tên địa chỉ | Không | Ví dụ: Kho Yên Nghĩa, Công trình Sơn La |
| Địa chỉ chi tiết | Có | Nơi giao hàng |
| Người nhận mặc định | Không | Tên người nhận |
| Điện thoại người nhận | Không | SĐT người nhận |
| Ghi chú | Không | Ghi chú giao hàng |
| Mặc định | Không | Địa chỉ mặc định của khách |

---

## 4.3. Danh mục hàng hóa

### 4.3.1. Mục đích

Quản lý các mặt hàng bán, đơn vị tính, quy cách, giá bán, giá vốn.

### 4.3.2. Trường dữ liệu

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Mã hàng | Có | Tự sinh hoặc nhập tay |
| Tên hàng | Có | Tên sản phẩm |
| Nhóm hàng | Có | EPS, XPS, thùng xốp, PE Foam... |
| Đơn vị tính | Có | Tấm, m², thùng, túi, kg, chuyến |
| Chiều dài | Không | Đơn vị mm |
| Chiều rộng | Không | Đơn vị mm |
| Chiều cao / độ dày | Không | Đơn vị mm |
| Tỷ trọng | Không | kg/m³ |
| Quy cách hiển thị | Không | Ví dụ: 1000x2000x50mm |
| Giá bán mặc định | Không | Giá bán tham khảo |
| Giá nhập / giá vốn | Không | Dùng để tính lợi nhuận |
| Thuế suất mặc định | Không | 0%, 8%, 10% |
| Ghi chú | Không | Thông tin khác |
| Trạng thái | Có | Đang sử dụng / Ngừng sử dụng |

### 4.3.3. Yêu cầu nghiệp vụ

- Cho phép chọn hàng hóa từ danh mục khi lập báo giá/đơn hàng.
- Cho phép nhập hàng hóa tự do nếu chưa có trong danh mục.
- Với hàng dạng tấm, cần hỗ trợ nhập:
  - Dài.
  - Rộng.
  - Dày/cao.
  - Tỷ trọng.
  - Số lượng tấm.
  - Số lượng m².
- Với hàng dạng thùng/túi/kg, tính tiền theo số lượng đơn vị.
- Cho phép lưu hàng hóa mới phát sinh vào danh mục.

---

## 4.4. Danh mục nhóm hàng

Các nhóm hàng đề xuất:

- Tấm xốp EPS.
- Tấm xốp XPS.
- Tấm xốp PE Foam.
- Tấm cao su non.
- Thùng xốp.
- Da gel.
- Bông khoáng.
- Bông thủy tinh.
- Vận chuyển.
- Khác.

---

## 4.5. Danh mục đơn vị tính

Ví dụ:

- Tấm.
- m².
- m³.
- Thùng.
- Túi.
- Kg.
- Chuyến.
- Bộ.
- Cái.

---

## 4.6. Danh mục thuế suất

Các mức thuế cần hỗ trợ:

- Không thuế.
- 0%.
- 8%.
- 10%.
- Tùy chỉnh.

---

## 4.7. Danh mục tài khoản ngân hàng

### 4.7.1. Mục đích

Dùng để in thông tin thanh toán và QR chuyển khoản trên báo giá, đơn hàng, biên bản bàn giao.

### 4.7.2. Trường dữ liệu

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Tên chủ tài khoản | Có | Tên công ty hoặc cá nhân |
| Số tài khoản | Có | STK ngân hàng |
| Ngân hàng | Có | Tên ngân hàng |
| Chi nhánh | Không | Chi nhánh ngân hàng |
| Ảnh QR | Không | QR chuyển khoản |
| Mặc định | Không | Tài khoản mặc định khi in chứng từ |
| Trạng thái | Có | Đang sử dụng / Ngừng sử dụng |

---

## 4.8. Danh mục điều khoản mẫu

Dùng để chọn nhanh khi lập báo giá, đơn hàng, biên bản bàn giao.

Ví dụ:

```text
Bên bán lên hàng, bên mua xuống hàng.
```

```text
Bên mua thanh toán 100% giá trị đơn hàng trước khi nhận hàng.
```

```text
Bên cung cấp đã bàn giao cho bên tiếp nhận vật tư, hàng hóa theo đúng chủng loại đã thỏa thuận.
Bên tiếp nhận đã kiểm tra và nhận toàn bộ số lượng hàng trên.
```

```text
Biên bản được lập thành 02 bản, mỗi bên giữ 01 bản, có giá trị pháp lý như nhau.
```

---

## 4.9. Cấu hình thông tin công ty

Thông tin dùng để in trên chứng từ:

| Trường | Mô tả |
|---|---|
| Tên công ty | Tên đầy đủ của công ty |
| Logo | Logo in trên chứng từ |
| Địa chỉ | Địa chỉ công ty |
| MST | Mã số thuế |
| Điện thoại | Số điện thoại liên hệ |
| Email | Email công ty |
| Website | Website công ty |
| Người đại diện | Nếu cần |
| Danh sách nhóm sản phẩm | Hiển thị checkbox trên mẫu in |

---

# 5. Chức năng lập báo giá

## 5.1. Mục tiêu

Cho phép người dùng lập báo giá cho khách hàng, nhập danh sách hàng hóa, tính tiền, thuế, tổng cộng và lợi nhuận nội bộ.

## 5.2. Luồng nghiệp vụ

```text
Tạo báo giá mới
→ Chọn khách hàng
→ Nhập thông tin giao hàng
→ Nhập danh sách hàng hóa
→ Nhập đơn giá, số lượng, thuế, cước
→ Hệ thống tự tính tổng tiền
→ Lưu báo giá
→ In báo giá hoặc gửi khách
→ Khách xác nhận
→ Chuyển thành đơn hàng
```

## 5.3. Trường dữ liệu báo giá

### 5.3.1. Thông tin chung

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Số báo giá | Có | Tự sinh theo cấu hình |
| Ngày báo giá | Có | Mặc định ngày hiện tại |
| Người lập | Có | Người dùng đang đăng nhập |
| Trạng thái | Có | Nháp, đã gửi, đã xác nhận, đã chuyển đơn, hủy |
| Mẫu báo giá | Không | Báo giá thường / báo giá kiêm xác nhận đơn hàng |

### 5.3.2. Thông tin khách hàng

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Khách hàng | Có | Chọn từ danh mục |
| Tên đơn vị mua hàng | Có | Tự điền từ khách hàng |
| Mã số thuế | Không | MST khách |
| Địa chỉ | Không | Địa chỉ khách |
| Người liên hệ | Không | Người đặt hàng |
| Điện thoại | Không | SĐT người đặt |
| Email | Không | Email khách |

### 5.3.3. Thông tin giao hàng

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Địa chỉ giao hàng | Có | Nơi nhận hàng |
| Người nhận hàng | Không | Người nhận thực tế |
| Điện thoại người nhận | Không | SĐT người nhận |
| Thời gian giao hàng | Không | Ngày/giờ dự kiến giao |
| Ghi chú giao hàng | Không | Ghi chú vận chuyển |

### 5.3.4. Chi tiết hàng hóa

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| STT | Có | Tự đánh số |
| Hàng hóa | Có | Chọn hoặc nhập tự do |
| Nội dung | Có | Tên/quy cách hàng hóa |
| ĐVT | Có | Đơn vị tính |
| Số lượng | Có | Số lượng bán |
| Đơn giá bán | Có | Giá bán cho khách |
| Thành tiền | Có | Tự tính |
| Chiều dài | Không | Nếu hàng dạng tấm |
| Chiều rộng | Không | Nếu hàng dạng tấm |
| Chiều cao/dày | Không | Nếu hàng dạng tấm |
| Tỷ trọng | Không | Nếu có |
| Số lượng m² | Không | Tự tính hoặc nhập |
| Giá nhập | Không | Chỉ nội bộ |
| Thành tiền nhập | Không | Chỉ nội bộ |
| Lợi nhuận | Không | Chỉ nội bộ |
| Ghi chú | Không | Ghi chú dòng hàng |

### 5.3.5. Tổng tiền

| Trường | Mô tả |
|---|---|
| Cộng tiền hàng | Tổng thành tiền các dòng |
| Chiết khấu | Nếu có |
| Cước vận chuyển | Nếu tính riêng |
| Thuế suất GTGT | 0%, 8%, 10% hoặc tùy chỉnh |
| Tiền thuế GTGT | Tự tính |
| Tổng cộng | Tiền hàng + thuế + cước - chiết khấu |
| Đã tạm ứng | Nếu khách đã trả trước |
| Còn thanh toán | Tổng cộng - đã tạm ứng |

### 5.3.6. Thông tin nội bộ

| Trường | Mô tả |
|---|---|
| Tổng giá vốn | Tổng thành tiền nhập |
| Lợi nhuận gộp | Doanh thu - giá vốn |
| Tỷ suất lợi nhuận | Lợi nhuận / doanh thu |
| Ghi chú nội bộ | Chỉ nhân viên nội bộ xem |
| Người phụ trách | Nhân viên kinh doanh |

## 5.4. Trạng thái báo giá

| Trạng thái | Ý nghĩa |
|---|---|
| Nháp | Báo giá đang soạn |
| Đã gửi khách | Báo giá đã được in/gửi |
| Khách đã xác nhận | Khách đồng ý mua |
| Đã chuyển đơn hàng | Đã tạo đơn hàng từ báo giá |
| Hủy | Báo giá không thực hiện |

## 5.5. Quy tắc nghiệp vụ

- Báo giá phải có ít nhất 1 dòng hàng.
- Không cho lưu nếu số lượng hoặc đơn giá âm.
- Cảnh báo nếu đơn giá bán thấp hơn giá nhập.
- Cảnh báo nếu lợi nhuận âm.
- Báo giá đã chuyển thành đơn hàng không được sửa nội dung chính nếu không có quyền.
- Có thể nhân bản báo giá cũ để tạo báo giá mới.
- Có thể chuyển báo giá thành đơn hàng.
- Bản in gửi khách không hiển thị giá nhập, giá vốn, lợi nhuận.

---

# 6. Chức năng in báo giá

## 6.1. Mục tiêu

In báo giá đúng mẫu hiện tại của công ty, hỗ trợ xem trước và xuất PDF.

## 6.2. Mẫu in cần hỗ trợ

### 6.2.1. Mẫu bảng báo giá hàng hóa

Thông tin hiển thị:

- Logo công ty.
- Tên công ty.
- Địa chỉ.
- MST.
- Điện thoại.
- Email.
- Website.
- Danh sách nhóm sản phẩm.
- Tiêu đề: `BẢNG BÁO GIÁ HÀNG HÓA`.
- Ngày báo giá.
- Đơn vị mua hàng.
- Địa chỉ giao hàng.
- Điện thoại.
- Hàng hóa cung cấp.
- Bảng chi tiết hàng hóa.
- Cộng tiền hàng.
- Thuế GTGT.
- Tổng cộng.
- Lưu ý.
- Thông tin thanh toán.
- Thông tin chuyển khoản.
- Khu vực chữ ký bên mua / bên bán.

### 6.2.2. Mẫu bảng báo giá kiêm xác nhận đơn hàng

Thông tin hiển thị:

- Toàn bộ thông tin của báo giá.
- Tiêu đề: `BẢNG BÁO GIÁ KIÊM XÁC NHẬN ĐƠN HÀNG`.
- Số báo giá.
- Điều khoản giao hàng.
- Điều khoản thanh toán.
- Khu vực xác nhận hai bên.

## 6.3. Yêu cầu chức năng

- Cho phép xem trước trước khi in.
- Cho phép xuất PDF.
- Cho phép in khổ A4.
- Cho phép chọn mẫu in.
- Cho phép chọn tài khoản ngân hàng hiển thị.
- Cho phép hiển thị hoặc ẩn QR thanh toán.
- Cho phép hiển thị hoặc ẩn thuế GTGT.
- Cho phép hiển thị hoặc ẩn cước vận chuyển.
- Không hiển thị giá nhập/lợi nhuận trên bản in gửi khách.
- Định dạng tiền theo kiểu Việt Nam, ví dụ: `2.829.600`.
- Định dạng ngày: `Hà Nội, ngày ... tháng ... năm ...`.

---

# 7. Chức năng lập đơn hàng

## 7.1. Mục tiêu

Quản lý đơn hàng sau khi khách xác nhận báo giá hoặc tạo đơn hàng trực tiếp.

## 7.2. Cách tạo đơn hàng

### 7.2.1. Tạo từ báo giá

Luồng xử lý:

```text
Chọn báo giá đã xác nhận
→ Bấm Chuyển thành đơn hàng
→ Hệ thống copy dữ liệu báo giá sang đơn hàng
→ Người dùng kiểm tra/chỉnh sửa thông tin nếu cần
→ Lưu đơn hàng
```

### 7.2.2. Tạo trực tiếp

Luồng xử lý:

```text
Tạo đơn hàng mới
→ Chọn khách hàng
→ Nhập thông tin giao hàng
→ Nhập danh sách hàng hóa
→ Nhập thanh toán
→ Lưu đơn hàng
```

## 7.3. Trường dữ liệu đơn hàng

### 7.3.1. Thông tin chung

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Mã đơn hàng | Có | Tự sinh |
| Ngày đơn hàng | Có | Mặc định ngày hiện tại |
| Nguồn báo giá | Không | Nếu tạo từ báo giá |
| Người lập | Có | Người dùng tạo đơn |
| Trạng thái đơn hàng | Có | Mới tạo, đang chuẩn bị, chờ giao, đã giao, hoàn thành, hủy |
| Trạng thái thanh toán | Có | Chưa thanh toán, thanh toán một phần, đã thanh toán |

### 7.3.2. Thông tin khách hàng và giao hàng

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Khách hàng | Có | Khách mua hàng |
| Mã số thuế | Không | MST nếu có |
| Địa chỉ khách hàng | Không | Địa chỉ công ty |
| Người đặt hàng | Không | Người liên hệ |
| Điện thoại người đặt | Không | SĐT người đặt |
| Người nhận hàng | Không | Người nhận thực tế |
| Điện thoại người nhận | Không | SĐT người nhận |
| Địa chỉ nhận hàng | Có | Nơi giao hàng |
| Thời gian giao hàng | Không | Ngày/giờ hẹn giao |
| Kho xuất | Không | Nếu có quản lý kho |

### 7.3.3. Chi tiết hàng hóa

Tương tự báo giá, gồm:

- STT.
- Hàng hóa.
- Nội dung/quy cách.
- ĐVT.
- Số lượng.
- Đơn giá.
- Thành tiền.
- Ghi chú.
- Giá nhập.
- Thành tiền nhập.
- Lợi nhuận nội bộ.

### 7.3.4. Thanh toán

| Trường | Mô tả |
|---|---|
| Cộng tiền hàng | Tổng tiền các dòng |
| Thuế GTGT | Tiền thuế |
| Cước vận chuyển | Nếu có |
| Tổng cộng | Tổng phải thu |
| Đã tạm ứng | Số tiền khách đã trả trước |
| Đã thanh toán | Tổng tiền khách đã thanh toán |
| Còn thanh toán | Tổng cộng - đã thanh toán |
| Hình thức thanh toán | Tiền mặt / chuyển khoản |
| Tài khoản nhận tiền | Tài khoản ngân hàng nhận tiền |
| Ghi chú thanh toán | Ghi chú nếu có |

## 7.4. Trạng thái đơn hàng

| Trạng thái | Ý nghĩa |
|---|---|
| Mới tạo | Đơn hàng vừa được lập |
| Đang chuẩn bị | Đang chuẩn bị/sản xuất hàng |
| Chờ giao | Hàng đã sẵn sàng giao |
| Đang giao | Đang vận chuyển |
| Đã bàn giao | Đã lập biên bản bàn giao |
| Hoàn thành | Đã giao và thanh toán xong |
| Hủy | Đơn hàng bị hủy |

## 7.5. Quy tắc nghiệp vụ

- Đơn hàng phải có khách hàng và ít nhất 1 dòng hàng.
- Đơn hàng tạo từ báo giá cần lưu liên kết với báo giá gốc.
- Khi đơn hàng đã bàn giao, không được sửa số lượng/đơn giá nếu không có quyền.
- Khi đơn hàng hoàn thành, phải khóa chỉnh sửa.
- Đơn hàng chưa thanh toán đủ mà muốn hoàn thành thì cần cảnh báo.
- Cho phép cập nhật trạng thái giao hàng.
- Cho phép cập nhật thanh toán nhiều lần.
- Cho phép in đơn hàng, biên bản bàn giao, phiếu xuất kho từ đơn hàng.

---

# 8. Chức năng in đơn hàng

## 8.1. Mục tiêu

In phiếu đơn hàng hoặc xác nhận đơn hàng để gửi khách hoặc lưu nội bộ.

## 8.2. Các mẫu in

### 8.2.1. Mẫu gửi khách

Không hiển thị:

- Giá nhập.
- Giá vốn.
- Lợi nhuận.
- Ghi chú nội bộ.

Hiển thị:

- Thông tin công ty.
- Thông tin khách hàng.
- Thông tin giao hàng.
- Danh sách hàng hóa.
- Tổng tiền.
- Thuế.
- Cước vận chuyển.
- Đã thanh toán.
- Còn thanh toán.
- Điều khoản giao hàng.
- Điều khoản thanh toán.
- Chữ ký xác nhận.

### 8.2.2. Mẫu nội bộ

Có thể hiển thị thêm:

- Giá nhập.
- Thành tiền nhập.
- Lợi nhuận từng dòng.
- Tổng lợi nhuận.
- Người phụ trách.
- Ghi chú nội bộ.

## 8.3. Yêu cầu chức năng

- In từ màn hình chi tiết đơn hàng.
- Cho phép xem trước trước khi in.
- Cho phép xuất PDF.
- Cho phép chọn mẫu gửi khách hoặc nội bộ.
- Cho phép chọn hiển thị QR thanh toán.
- Cho phép chọn tài khoản ngân hàng nhận tiền.

---

# 9. Chức năng in biên bản bàn giao

## 9.1. Mục tiêu

Tự động sinh biên bản bàn giao từ đơn hàng, tránh nhập lại thủ công.

## 9.2. Mẫu in cần hỗ trợ

### 9.2.1. Mẫu biên bản bàn giao hàng hóa

Tiêu đề:

```text
BIÊN BẢN BÀN GIAO HÀNG HÓA
```

Thông tin hiển thị:

- Logo công ty.
- Tên công ty.
- Địa chỉ.
- MST.
- Điện thoại.
- Email.
- Ngày bàn giao.
- Đơn vị mua hàng.
- Địa chỉ giao hàng.
- Điện thoại.
- Hàng hóa cung cấp.
- Danh sách hàng hóa.
- Tổng tiền.
- Lưu ý.
- Thanh toán.
- Tình trạng hàng hóa.
- Chữ ký người nhận hàng.
- Chữ ký người giao hàng.

### 9.2.2. Mẫu biên bản bàn giao hàng kiêm phiếu xuất kho

Tiêu đề:

```text
BIÊN BẢN BÀN GIAO HÀNG KIÊM PHIẾU XUẤT KHO
```

Thông tin hiển thị:

- Thông tin công ty bán.
- Ngày chứng từ.
- Đơn vị mua hàng.
- Địa chỉ.
- MST.
- Điện thoại người đặt.
- Điện thoại người nhận.
- Địa chỉ nhận hàng.
- Thời gian nhận hàng.
- Hàng hóa cung cấp.
- Bảng chi tiết hàng hóa.
- Cộng tiền hàng.
- Thuế GTGT.
- Tổng cộng.
- Đã tạm ứng.
- Còn thanh toán.
- Ghi chú.
- QR chuyển khoản.
- Tình trạng hàng hóa.
- Xác nhận bên mua.
- Xác nhận bên bán.

## 9.3. Luồng nghiệp vụ

```text
Vào chi tiết đơn hàng
→ Bấm Lập biên bản bàn giao
→ Hệ thống lấy dữ liệu từ đơn hàng
→ Người dùng kiểm tra/chỉnh sửa thông tin bàn giao
→ Xem trước PDF
→ In hoặc tải PDF
→ Cập nhật trạng thái đơn hàng là Đã bàn giao
```

## 9.4. Trường dữ liệu biên bản bàn giao

| Trường | Nguồn dữ liệu |
|---|---|
| Số biên bản | Tự sinh |
| Ngày bàn giao | Người dùng chọn |
| Đơn hàng liên quan | Từ đơn hàng |
| Đơn vị mua hàng | Từ khách hàng |
| Địa chỉ giao hàng | Từ đơn hàng |
| Người nhận | Từ đơn hàng hoặc nhập tay |
| Điện thoại người nhận | Từ đơn hàng hoặc nhập tay |
| Hàng hóa cung cấp | Từ đơn hàng |
| Danh sách hàng hóa | Từ chi tiết đơn hàng |
| Cộng tiền hàng | Tự tính |
| Thuế GTGT | Từ đơn hàng |
| Tổng cộng | Tự tính |
| Đã tạm ứng | Từ thanh toán |
| Còn thanh toán | Tự tính |
| Ghi chú | Chọn từ mẫu hoặc nhập tay |
| QR chuyển khoản | Từ cấu hình ngân hàng |
| Người giao hàng | Nhập hoặc chọn người dùng |

## 9.5. Quy tắc nghiệp vụ

- Chỉ lập biên bản bàn giao cho đơn hàng có trạng thái hợp lệ.
- Biên bản lấy dữ liệu từ đơn hàng nhưng cho phép chỉnh một số thông tin như:
  - Ngày bàn giao.
  - Người nhận.
  - Điện thoại người nhận.
  - Ghi chú.
  - Số tiền đã tạm ứng.
- Sau khi in/xác nhận bàn giao, có thể cập nhật trạng thái đơn hàng thành `Đã bàn giao`.
- Biên bản đã xác nhận không được sửa nếu không có quyền.
- Có thể in lại biên bản từ lịch sử chứng từ.

---

# 10. Chức năng quản lý thanh toán và công nợ

## 10.1. Mục tiêu

Theo dõi số tiền khách đã thanh toán, còn phải thu và lịch sử thanh toán của từng đơn hàng.

## 10.2. Trường dữ liệu thanh toán

| Trường | Bắt buộc | Mô tả |
|---|---:|---|
| Mã giao dịch | Có | Tự sinh |
| Đơn hàng | Có | Đơn hàng liên quan |
| Ngày thanh toán | Có | Ngày nhận tiền |
| Số tiền | Có | Số tiền khách thanh toán |
| Hình thức thanh toán | Có | Tiền mặt / chuyển khoản |
| Tài khoản nhận | Không | Nếu chuyển khoản |
| Người ghi nhận | Có | Người dùng nhập |
| Ghi chú | Không | Nội dung thanh toán |

## 10.3. Trạng thái thanh toán

| Trạng thái | Điều kiện |
|---|---|
| Chưa thanh toán | Đã thanh toán = 0 |
| Thanh toán một phần | Đã thanh toán > 0 và < Tổng cộng |
| Đã thanh toán | Đã thanh toán >= Tổng cộng |

## 10.4. Quy tắc nghiệp vụ

- Một đơn hàng có thể có nhiều lần thanh toán.
- Tổng đã thanh toán bằng tổng các giao dịch thanh toán hợp lệ.
- Còn thanh toán = Tổng cộng - Đã thanh toán.
- Không cho nhập số tiền thanh toán âm.
- Cảnh báo nếu số tiền thanh toán vượt quá tổng còn phải thu.
- Khi đơn hàng thanh toán đủ, trạng thái thanh toán tự chuyển thành `Đã thanh toán`.

---

# 11. Chức năng tổng hợp báo cáo

## 11.1. Báo cáo tổng quan

### Chỉ tiêu hiển thị

- Doanh thu hôm nay.
- Doanh thu tháng này.
- Số đơn hàng hôm nay.
- Số đơn hàng tháng này.
- Tổng tiền còn phải thu.
- Số đơn hàng chờ giao.
- Số đơn hàng đã giao.
- Lợi nhuận tháng này.
- Top khách hàng theo doanh thu.
- Top sản phẩm bán chạy.

### Bộ lọc

- Từ ngày / đến ngày.
- Chi nhánh/kho nếu có.
- Người phụ trách.
- Nhóm hàng.

---

## 11.2. Báo cáo doanh thu

### Chỉ tiêu

| Chỉ tiêu | Mô tả |
|---|---|
| Tổng doanh thu | Tổng giá trị bán |
| Tổng tiền hàng | Chưa gồm thuế nếu cần |
| Tổng thuế GTGT | Tổng tiền thuế |
| Tổng cước vận chuyển | Tổng cước thu khách |
| Số đơn hàng | Tổng số đơn phát sinh |
| Giá trị trung bình đơn hàng | Doanh thu / số đơn |

### Bộ lọc

- Theo ngày.
- Theo tháng.
- Theo khách hàng.
- Theo nhóm hàng.
- Theo sản phẩm.
- Theo nhân viên.
- Theo trạng thái đơn hàng.

---

## 11.3. Báo cáo lợi nhuận

### Chỉ tiêu

| Chỉ tiêu | Mô tả |
|---|---|
| Thành tiền bán | Tổng doanh thu bán hàng |
| Giá vốn | Tổng giá nhập |
| Lợi nhuận gộp | Thành tiền bán - giá vốn |
| Cước vận chuyển | Cước vận chuyển nếu tính chi phí |
| Lợi nhuận sau cước | Lợi nhuận gộp - chi phí vận chuyển |
| Tỷ suất lợi nhuận | Lợi nhuận / doanh thu |

### Cột báo cáo đề xuất

| Cột | Mô tả |
|---|---|
| Ngày tháng | Ngày đơn hàng |
| Khách hàng | Tên khách |
| Địa chỉ giao hàng | Nơi giao |
| Hàng hóa | Tên hàng |
| Kích thước | Rộng, dài, cao/dày |
| Tỷ trọng | kg/m³ |
| Số lượng m² | Tổng diện tích |
| Số lượng tấm | Tổng số tấm |
| Đơn giá bán | Giá bán |
| Thành tiền bán | Doanh thu |
| Cước vận chuyển | Cước |
| Giá nhập | Giá vốn đơn vị |
| Thành tiền nhập | Tổng giá vốn |
| Chênh lệch | Lợi nhuận |
| Ghi chú | Ghi chú đơn hàng |

---

## 11.4. Báo cáo công nợ

### Chỉ tiêu

| Chỉ tiêu | Mô tả |
|---|---|
| Tổng giá trị đơn hàng | Tổng tiền phải thu |
| Đã thanh toán | Tổng tiền khách đã thanh toán |
| Còn phải thu | Công nợ còn lại |
| Số đơn chưa thanh toán | Đơn có công nợ |
| Số đơn đã thanh toán đủ | Đơn hoàn tất thanh toán |

### Bộ lọc

- Khách hàng.
- Từ ngày / đến ngày.
- Trạng thái thanh toán.
- Người phụ trách.

---

## 11.5. Báo cáo giao hàng

### Chỉ tiêu

| Chỉ tiêu | Mô tả |
|---|---|
| Đơn chờ giao | Đơn chưa bàn giao |
| Đơn đang giao | Đơn đang vận chuyển |
| Đơn đã giao | Đơn đã bàn giao |
| Đơn quá hạn giao | Đơn quá thời gian hẹn giao |
| Số biên bản đã in | Tổng biên bản bàn giao |

### Bộ lọc

- Ngày giao.
- Trạng thái giao hàng.
- Người giao hàng.
- Khách hàng.
- Khu vực giao hàng.

---

## 11.6. Báo cáo sản phẩm bán chạy

### Chỉ tiêu

| Chỉ tiêu | Mô tả |
|---|---|
| Sản phẩm | Tên hàng hóa |
| Nhóm hàng | Nhóm sản phẩm |
| Số lượng bán | Tổng số lượng |
| Diện tích bán | Tổng m² nếu có |
| Doanh thu | Tổng doanh thu |
| Giá vốn | Tổng giá vốn |
| Lợi nhuận | Doanh thu - giá vốn |
| Tỷ suất lợi nhuận | Lợi nhuận / doanh thu |

---

# 12. Màn hình phần mềm

## 12.1. Màn hình danh sách báo giá

### Cột hiển thị

| Cột |
|---|
| Số báo giá |
| Ngày báo giá |
| Khách hàng |
| Người liên hệ |
| Số điện thoại |
| Tổng tiền |
| Trạng thái |
| Người lập |
| Ngày tạo |
| Thao tác |

### Chức năng

- Thêm báo giá.
- Sửa báo giá.
- Xem chi tiết.
- In báo giá.
- Nhân bản báo giá.
- Chuyển thành đơn hàng.
- Hủy báo giá.
- Tìm kiếm.
- Lọc theo trạng thái.
- Lọc theo ngày.
- Lọc theo người lập.

---

## 12.2. Màn hình lập báo giá

### Khu vực thông tin

1. Thông tin chung.
2. Thông tin khách hàng.
3. Thông tin giao hàng.
4. Chi tiết hàng hóa.
5. Tổng tiền.
6. Điều khoản.
7. Thông tin nội bộ.
8. Nút thao tác.

### Nút thao tác

- Lưu nháp.
- Lưu.
- Lưu và in.
- In báo giá.
- Gửi khách.
- Chuyển thành đơn hàng.
- Hủy.

---

## 12.3. Màn hình danh sách đơn hàng

### Cột hiển thị

| Cột |
|---|
| Mã đơn hàng |
| Ngày đơn |
| Khách hàng |
| Địa chỉ giao hàng |
| Tổng tiền |
| Đã thanh toán |
| Còn thanh toán |
| Trạng thái giao hàng |
| Trạng thái thanh toán |
| Người lập |
| Thao tác |

### Chức năng

- Thêm đơn hàng.
- Xem chi tiết.
- Sửa đơn hàng.
- In đơn hàng.
- In biên bản bàn giao.
- In phiếu xuất kho.
- Cập nhật thanh toán.
- Cập nhật trạng thái giao hàng.
- Hủy đơn hàng.

---

## 12.4. Màn hình chi tiết đơn hàng

### Nội dung hiển thị

- Thông tin đơn hàng.
- Thông tin khách hàng.
- Thông tin giao hàng.
- Danh sách hàng hóa.
- Tổng tiền.
- Thanh toán.
- Lịch sử thanh toán.
- Lịch sử chứng từ.
- Lịch sử trạng thái.
- Ghi chú nội bộ.

### Nút thao tác

- In đơn hàng.
- In báo giá.
- In biên bản bàn giao.
- In phiếu xuất kho.
- Cập nhật thanh toán.
- Đánh dấu đã giao.
- Hoàn thành đơn.
- Hủy đơn.

---

## 12.5. Màn hình báo cáo

### Các tab báo cáo

1. Tổng quan.
2. Doanh thu.
3. Lợi nhuận.
4. Công nợ.
5. Giao hàng.
6. Sản phẩm.
7. Khách hàng.

### Chức năng chung

- Lọc dữ liệu.
- Xem biểu đồ.
- Xem bảng chi tiết.
- Xuất Excel.
- In báo cáo.
- Xuất PDF.

---

# 13. Quy tắc tính toán

## 13.1. Tính diện tích với hàng dạng tấm

```text
Diện tích 1 tấm = Dài x Rộng / 1.000.000
```

Trong đó:

- Dài tính bằng mm.
- Rộng tính bằng mm.
- Kết quả tính ra m².

Ví dụ:

```text
Dài = 2000 mm
Rộng = 1000 mm

Diện tích 1 tấm = 2000 x 1000 / 1.000.000 = 2 m²
```

## 13.2. Tính số lượng m²

```text
Số lượng m² = Diện tích 1 tấm x Số lượng tấm
```

## 13.3. Tính thành tiền

### Trường hợp bán theo số lượng

```text
Thành tiền = Số lượng x Đơn giá
```

### Trường hợp bán theo m²

```text
Thành tiền = Số lượng m² x Đơn giá/m²
```

## 13.4. Tính tổng tiền

```text
Cộng tiền hàng = Tổng thành tiền các dòng hàng
```

```text
Tiền thuế GTGT = Cộng tiền hàng x Thuế suất
```

```text
Tổng cộng = Cộng tiền hàng + Tiền thuế GTGT + Cước vận chuyển - Chiết khấu
```

```text
Còn thanh toán = Tổng cộng - Đã thanh toán
```

## 13.5. Tính lợi nhuận

```text
Thành tiền nhập = Số lượng x Giá nhập
```

```text
Lợi nhuận dòng = Thành tiền bán - Thành tiền nhập
```

```text
Tổng lợi nhuận = Tổng thành tiền bán - Tổng thành tiền nhập
```

```text
Tỷ suất lợi nhuận = Tổng lợi nhuận / Tổng doanh thu
```

---

# 14. Quy tắc sinh mã chứng từ

## 14.1. Mã báo giá

Đề xuất:

```text
BG-YYMMDD-0001
```

Ví dụ:

```text
BG-260509-0001
```

Hoặc theo mẫu hiện tại:

```text
080526/BGTP
```

## 14.2. Mã đơn hàng

```text
DH-YYMMDD-0001
```

## 14.3. Mã biên bản bàn giao

```text
BBBG-YYMMDD-0001
```

## 14.4. Mã phiếu xuất kho

```text
PXK-YYMMDD-0001
```

## 14.5. Yêu cầu

- Mã chứng từ tự tăng theo ngày hoặc theo tháng.
- Không cho phép trùng mã.
- Cho phép cấu hình định dạng mã.
- Cho phép reset số thứ tự theo ngày/tháng/năm tùy cấu hình.

---

# 15. Yêu cầu in ấn và xuất file

## 15.1. Loại chứng từ cần in

| Loại chứng từ | PDF | In trực tiếp | Excel |
|---|---:|---:|---:|
| Báo giá | Có | Có | Có thể có |
| Đơn hàng | Có | Có | Có thể có |
| Biên bản bàn giao | Có | Có | Không bắt buộc |
| Phiếu xuất kho | Có | Có | Không bắt buộc |
| Báo cáo | Có | Có | Có |

## 15.2. Yêu cầu mẫu in

- Khổ giấy A4.
- Logo nằm trên đầu chứng từ.
- Thông tin công ty nằm đầu trang.
- Tiêu đề chứng từ căn giữa, in đậm.
- Bảng chi tiết có đường viền rõ ràng.
- Có phần tổng tiền.
- Có phần ghi chú/lưu ý.
- Có phần thông tin thanh toán.
- Có thể hiển thị QR chuyển khoản.
- Có khu vực chữ ký hai bên.
- Tiền tệ định dạng kiểu Việt Nam.
- Ngày tháng định dạng tiếng Việt.
- Mẫu in cần giống chứng từ hiện tại nhất có thể.

---

# 16. Kiểm soát dữ liệu

## 16.1. Bắt buộc nhập

Các thông tin bắt buộc khi lập chứng từ:

- Khách hàng.
- Ngày chứng từ.
- Địa chỉ giao hàng.
- Ít nhất 1 dòng hàng hóa.
- Đơn vị tính.
- Số lượng.
- Đơn giá.

## 16.2. Cảnh báo

Hệ thống cần cảnh báo khi:

- Số lượng bằng 0.
- Đơn giá bằng 0.
- Giá bán thấp hơn giá nhập.
- Lợi nhuận âm.
- Đơn hàng chưa thanh toán đủ nhưng muốn hoàn thành.
- Đơn hàng đã bàn giao nhưng người dùng muốn sửa hàng hóa.
- Thiếu địa chỉ giao hàng.
- Thiếu số điện thoại người nhận khi in biên bản bàn giao.

## 16.3. Không cho phép

Hệ thống không cho phép:

- Tạo chứng từ không có hàng hóa.
- Tạo mã chứng từ trùng.
- Xóa báo giá đã chuyển đơn hàng nếu không có quyền.
- Sửa đơn hàng đã hoàn thành nếu không mở khóa.
- Xóa đơn hàng đã có thanh toán nếu không có quyền.
- In chứng từ khi thiếu dữ liệu bắt buộc.

---

# 17. Phân quyền dữ liệu

## 17.1. Quyền xem giá vốn và lợi nhuận

Không phải người dùng nào cũng được xem:

- Giá nhập.
- Thành tiền nhập.
- Lợi nhuận dòng.
- Tổng lợi nhuận.
- Tỷ suất lợi nhuận.

Chỉ các vai trò được cấp quyền mới có thể xem các thông tin này.

## 17.2. Quyền sửa chứng từ

| Trạng thái chứng từ | Quyền sửa |
|---|---|
| Nháp | Được sửa |
| Đã gửi khách | Được sửa nếu có quyền |
| Đã chuyển đơn hàng | Không sửa nội dung chính nếu không có quyền |
| Đã bàn giao | Không sửa hàng hóa nếu không có quyền |
| Hoàn thành | Khóa chỉnh sửa |

## 17.3. Quyền báo cáo

- Nhân viên kinh doanh chỉ xem được dữ liệu của mình nếu cấu hình như vậy.
- Quản lý xem được toàn bộ dữ liệu.
- Kế toán xem được doanh thu, công nợ.
- Chỉ người có quyền mới xem được báo cáo lợi nhuận.

---

# 18. Dữ liệu cần migrate từ Excel

## 18.1. Dữ liệu bán hàng

Từ file bán hàng theo tháng, cần chuyển đổi các thông tin sau:

| Dữ liệu Excel | Dữ liệu hệ thống |
|---|---|
| Ngày tháng | Ngày đơn hàng |
| Địa chỉ giao hàng | Địa chỉ giao |
| Kích thước | Quy cách hàng hóa |
| Tỷ trọng | Thuộc tính sản phẩm |
| Số lượng m² | Số lượng phụ |
| Số lượng tấm | Số lượng chính |
| Đơn giá | Đơn giá bán |
| Thành tiền | Thành tiền bán |
| Cước vận chuyển | Cước |
| Giá nhập | Giá vốn |
| Thành tiền nhập | Tổng giá vốn |
| Chênh lệch | Lợi nhuận |
| Số điện thoại | Thông tin liên hệ |
| Ghi chú | Ghi chú đơn hàng |

## 18.2. Lưu ý khi migrate

- File Excel có thể có nhiều dòng thuộc cùng một đơn hàng.
- Một số dòng có thể bỏ trống thông tin ngày, địa chỉ do kế thừa từ dòng trên.
- Cần chuẩn hóa khách hàng trước khi import.
- Cần chuẩn hóa hàng hóa/quy cách trước khi import.
- Cần kiểm tra dữ liệu trùng.
- Cần xác định rõ cột nào là doanh thu, cột nào là giá vốn.

---

# 19. Module hệ thống đề xuất

## 19.1. Module danh mục

Bao gồm:

- Khách hàng.
- Địa chỉ giao hàng.
- Hàng hóa.
- Nhóm hàng.
- Đơn vị tính.
- Thuế suất.
- Tài khoản ngân hàng.
- Điều khoản mẫu.
- Người dùng.
- Vai trò và phân quyền.

## 19.2. Module báo giá

Bao gồm:

- Danh sách báo giá.
- Thêm/sửa báo giá.
- Xem chi tiết báo giá.
- In báo giá.
- Nhân bản báo giá.
- Chuyển báo giá thành đơn hàng.
- Theo dõi trạng thái báo giá.

## 19.3. Module đơn hàng

Bao gồm:

- Danh sách đơn hàng.
- Thêm/sửa đơn hàng.
- Xem chi tiết đơn hàng.
- Cập nhật trạng thái.
- Cập nhật thanh toán.
- In đơn hàng.
- Hủy đơn hàng.

## 19.4. Module bàn giao/xuất kho

Bao gồm:

- Lập biên bản bàn giao.
- In biên bản bàn giao.
- In phiếu xuất kho.
- Theo dõi lịch sử bàn giao.

## 19.5. Module thanh toán/công nợ

Bao gồm:

- Ghi nhận thanh toán.
- Ghi nhận tạm ứng.
- Theo dõi công nợ.
- Lịch sử thanh toán.
- Báo cáo công nợ.

## 19.6. Module báo cáo

Bao gồm:

- Báo cáo tổng quan.
- Báo cáo doanh thu.
- Báo cáo lợi nhuận.
- Báo cáo công nợ.
- Báo cáo giao hàng.
- Báo cáo sản phẩm.
- Báo cáo khách hàng.

## 19.7. Module cấu hình

Bao gồm:

- Thông tin công ty.
- Logo.
- Mẫu in.
- QR thanh toán.
- Tài khoản ngân hàng.
- Quy tắc sinh mã.
- Cấu hình thuế.
- Cấu hình phân quyền.

---

# 20. Yêu cầu phi chức năng

## 20.1. Hiệu năng

- Danh sách báo giá/đơn hàng cần tải nhanh khi có nhiều dữ liệu.
- Hỗ trợ phân trang, tìm kiếm, lọc dữ liệu.
- Báo cáo theo tháng cần trả kết quả trong thời gian chấp nhận được.

## 20.2. Bảo mật

- Người dùng phải đăng nhập.
- Phân quyền theo vai trò.
- Chỉ người có quyền mới xem giá vốn/lợi nhuận.
- Ghi nhận lịch sử thao tác quan trọng.
- Không cho phép truy cập dữ liệu trái quyền.

## 20.3. Sao lưu dữ liệu

- Dữ liệu cần được sao lưu định kỳ.
- Có khả năng khôi phục khi xảy ra lỗi.
- Hạn chế mất dữ liệu chứng từ.

## 20.4. Nhật ký hệ thống

Cần ghi nhận lịch sử:

- Ai tạo chứng từ.
- Ai sửa chứng từ.
- Ai in chứng từ.
- Ai cập nhật thanh toán.
- Ai cập nhật trạng thái giao hàng.
- Thời điểm thực hiện thao tác.

## 20.5. Xuất nhập dữ liệu

- Cho phép export Excel danh sách báo giá.
- Cho phép export Excel danh sách đơn hàng.
- Cho phép export Excel báo cáo.
- Có thể import dữ liệu khách hàng.
- Có thể import dữ liệu hàng hóa.
- Có thể import dữ liệu bán hàng cũ từ Excel.

---

# 21. MVP đề xuất

## 21.1. Giai đoạn 1 - Chức năng lõi

Mục tiêu: Thay thế quy trình lập báo giá, đơn hàng, in chứng từ.

Bao gồm:

- Đăng nhập.
- Phân quyền cơ bản.
- Danh mục khách hàng.
- Danh mục hàng hóa.
- Lập báo giá.
- In báo giá PDF.
- Chuyển báo giá thành đơn hàng.
- Lập đơn hàng.
- In đơn hàng.
- In biên bản bàn giao.
- Báo cáo doanh thu/lợi nhuận cơ bản.

## 21.2. Giai đoạn 2 - Quản lý vận hành

Mục tiêu: Quản lý giao hàng, thanh toán, công nợ.

Bao gồm:

- Cập nhật trạng thái đơn hàng.
- Cập nhật trạng thái giao hàng.
- Ghi nhận thanh toán.
- Theo dõi công nợ.
- QR thanh toán.
- Lịch sử chỉnh sửa chứng từ.
- Cấu hình mẫu in.

## 21.3. Giai đoạn 3 - Báo cáo nâng cao

Mục tiêu: Phân tích hiệu quả kinh doanh.

Bao gồm:

- Dashboard tổng quan.
- Báo cáo lợi nhuận chi tiết.
- Báo cáo theo khách hàng.
- Báo cáo theo sản phẩm.
- Báo cáo theo nhân viên.
- Import dữ liệu Excel cũ.
- Export báo cáo nâng cao.

---

# 22. Các câu hỏi cần xác nhận thêm

1. Hệ thống có cần quản lý tồn kho thật không, hay chỉ cần in phiếu xuất kho?
2. Đơn giá bán chủ yếu tính theo tấm, m², thùng, kg hay tùy từng sản phẩm?
3. Giá nhập có cần quản lý theo từng lần nhập hàng không?
4. Thuế GTGT mặc định là 8% hay tùy từng đơn hàng?
5. Có cần duyệt báo giá nếu giá bán thấp hơn giá nhập không?
6. Có cần duyệt báo giá nếu lợi nhuận dưới mức quy định không?
7. Có cần quản lý nhiều kho không?
8. Có cần quản lý nhiều chi nhánh không?
9. Có cần gửi báo giá qua email/Zalo trực tiếp từ phần mềm không?
10. Có cần ký điện tử trên biên bản bàn giao không?
11. Có cần lưu file PDF sau mỗi lần in không?
12. Đơn hàng đã bàn giao có được sửa không?
13. Cước vận chuyển được tính là doanh thu, chi phí hay chỉ là khoản thu hộ?
14. QR thanh toán dùng chung cho toàn công ty hay chọn theo từng đơn hàng?
15. Báo cáo lợi nhuận có cho nhân viên kinh doanh xem không?

---

# 23. Kết luận

Phần mềm quản lý đơn hàng cần tập trung giải quyết quy trình chính:

```text
Lập báo giá
→ In báo giá
→ Khách xác nhận
→ Lập đơn hàng
→ In đơn hàng
→ In biên bản bàn giao / phiếu xuất kho
→ Cập nhật thanh toán
→ Tổng hợp báo cáo
```

Các yêu cầu quan trọng nhất:

1. Dữ liệu chỉ nhập một lần và được tái sử dụng cho nhiều chứng từ.
2. Mẫu in phải giống chứng từ hiện tại.
3. Hệ thống phải tự động tính tiền, thuế, tổng cộng, công nợ và lợi nhuận.
4. Phải phân quyền rõ người được xem giá vốn/lợi nhuận.
5. Phải theo dõi được trạng thái báo giá, đơn hàng, giao hàng, thanh toán.
6. Phải có báo cáo doanh thu, lợi nhuận, công nợ theo thời gian, khách hàng, sản phẩm.
7. Có thể mở rộng thêm quản lý kho, duyệt giá, import Excel và dashboard nâng cao ở các giai đoạn sau.
