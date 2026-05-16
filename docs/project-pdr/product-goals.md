# QLDonHang — Product Goals

## Mục tiêu sản phẩm
QLDonHang là phần mềm quản lý báo giá. Báo giá là chứng từ duy nhất xuyên suốt vòng đời bán hàng. Khi báo giá đạt trạng thái Đã xác nhận, hệ thống ghi nhận doanh thu cho nhân viên kinh doanh sở hữu báo giá đó.

## Đối tượng sử dụng
- **Sale**: tạo báo giá, gửi khách, theo dõi trạng thái, xác nhận khi khách OK.
- **Quản lý/Admin**: xem báo cáo doanh thu toàn hệ thống, hủy báo giá đã xác nhận khi cần.

## Phạm vi
- Lập, sửa, in báo giá (PDF/Excel).
- Theo dõi trạng thái báo giá: `Draft → Sent → Confirmed → Cancelled`.
- Khi flip sang Confirmed: snapshot thời điểm + người xác nhận, ghi nhận doanh thu cho owner sale.
- Báo cáo doanh thu theo sale, group theo `ConfirmedAt`, hiển thị Total (gồm thuế + cước) và Subtotal (thuần).
- Phân quyền: Sale tự confirm; chỉ admin/manager hủy báo giá đã Confirmed.
- Quản lý danh mục khách hàng, hàng hóa.

## Non-goals
- Không quản lý đơn hàng (Order) như chứng từ riêng.
- Không quản lý bàn giao, phiếu xuất kho, biên bản bàn giao.
- Không tracking thanh toán nhiều đợt, công nợ, tạm ứng.
- Không báo cáo công nợ, báo cáo giao hàng.
- Không gửi báo giá qua email/Zalo trực tiếp từ phần mềm (giai đoạn sau).
- Không quản lý tồn kho.

## Tài liệu liên quan
- Brainstorm: [../brainstorms/260515-1249-quotation-only-pivot/SUMMARY.md](../brainstorms/260515-1249-quotation-only-pivot/SUMMARY.md)
- BD doc cũ (đã archive): [../bd/archived/phan-tich-yeu-cau-phan-mem-quan-ly-don-hang.md](../bd/archived/phan-tich-yeu-cau-phan-mem-quan-ly-don-hang.md)
- Architecture: [../architecture/system-architecture.md](../architecture/system-architecture.md)
