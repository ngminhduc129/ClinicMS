# LUỒNG NGHIỆP VỤ VÀ NGƯỜI DÙNG (USER FLOW & BUSINESS LOGIC)

Tài liệu này mô tả chi tiết các luồng nghiệp vụ (Business Logic) và tương tác của người dùng (User Flow) trong hệ thống ClinicMS, dựa trên cấu trúc dữ liệu và giao diện hiện tại.

---

## 1. CÁC VAI TRÒ NGẦM ĐỊNH (IMPLICIT ROLES)
Mặc dù hệ thống hiện tại chưa chia quyền (RBAC - Role Based Access Control) bằng code, nhưng thiết kế UI và Data Model phục vụ cho 3 nhóm vai trò chính:
1. **Lễ tân / Thu ngân (Receptionist / Cashier):** Đón tiếp, tạo hồ sơ, thu tiền.
2. **Bác sĩ (Doctor):** Khám bệnh, chẩn đoán, kê đơn, chỉ định dịch vụ.
3. **Quản trị viên (Admin):** Quản lý danh mục (Thuốc, Dịch vụ, Bác sĩ), xem thống kê (Dashboard), cài đặt hệ thống.

---

## 2. LUỒNG NGHIỆP VỤ CỐT LÕI: KHÁM CHỮA BỆNH (END-TO-END CORE FLOW)

Đây là hành trình đầy đủ của một bệnh nhân từ lúc bước vào phòng khám đến khi ra về.

### Bước 2.1: Tiếp đón (Reception)
*   **Màn hình:** `PatientListView` (Bệnh Nhân).
*   **Hành động:**
    1. Lễ tân tìm kiếm bệnh nhân theo Tên/SĐT (Search/Lọc).
    2. Nếu là bệnh nhân mới ➔ Chọn "Thêm Bệnh Nhân" ➔ Nhập thông tin (Tên, Tuổi, Giới tính, SĐT, Địa chỉ).
    3. Mở tab "Hồ Sơ Khám" của bệnh nhân ➔ Tạo mới một `Visit` (Ca khám).
    4. Gán `Doctor` (Bác sĩ phụ trách) và chọn `VisitType` (Khám trực tiếp / Tái khám).
    5. Trạng thái mặc định của `Visit` là: **"Waiting" (Đang chờ)**.

### Bước 2.2: Khám bệnh lâm sàng (Examination)
*   **Màn hình:** `VisitListView` (Hồ Sơ Khám).
*   **Hành động:**
    1. Bác sĩ mở tab Hồ sơ khám, lọc các ca có trạng thái **"Chờ khám"**.
    2. Click vào ca khám của bệnh nhân, chuyển trạng thái sang **"In Progress" (Đang khám)**.
    3. **Nhập Sinh hiệu (Vitals):** Mạch, Huyết áp, Nhiệt độ, Cân nặng.
    4. **Nhập Lâm sàng:** Ghi nhận "Triệu chứng" (Symptoms) từ lời khai bệnh nhân.
    5. **Đưa ra Kết luận:** Nhập "Chẩn đoán" (Diagnosis).

### Bước 2.3: Chỉ định & Kê đơn (Treatment)
*   **Màn hình:** Nửa bên phải của `VisitListView` (Detail Panel).
*   **Hành động:**
    1. **Kê đơn thuốc:** Bác sĩ tìm thuốc trong ô tìm kiếm nhanh (có cảnh báo nếu hết tồn kho `StockQuantity <= 0`), nhập số lượng, cách dùng (Dosage) ➔ Lưu vào `PrescriptionDetail`.
    2. **Chỉ định dịch vụ:** (Hiện tại UI chưa tích hợp trực tiếp panel thêm dịch vụ nhanh vào Visit form, nhưng Database đã có `ServiceOrder`).
    3. Bác sĩ nhấn nút **Lưu Bệnh Án** (Icon màu xanh).
    4. Bác sĩ chuyển trạng thái ca khám sang **"Completed" (Hoàn thành)**.
    5. (Tùy chọn) Bác sĩ nhấn In Bệnh Án hoặc In Đơn Thuốc (PDF) cho bệnh nhân.

### Bước 2.4: Thanh toán & Xuất hóa đơn (Checkout & Billing)
*   **Màn hình:** `InvoiceListView` (Hóa Đơn).
*   **Hành động:**
    1. Lễ tân/Thu ngân mở tab Hóa Đơn. Hệ thống đã **tự động sinh ra một Hóa đơn** (Invoice) với tổng tiền dựa trên Đơn thuốc và Dịch vụ đã chọn ở Bước 2.3.
    2. Thu ngân thông báo số tiền (Total Amount) cho bệnh nhân.
    3. Thu ngân nhấn **Thanh Toán Ngay**.
    4. Nhập số tiền bệnh nhân đưa, chọn phương thức (Tiền mặt / Chuyển khoản).
    5. Nếu thanh toán đủ ➔ Trạng thái hóa đơn thành **"Paid" (Đã thanh toán)**. Nếu thiếu ➔ **"Partial" (Thanh toán 1 phần)**.
    6. Thu ngân nhấn **In Hóa Đơn** (PDF) và đưa cho bệnh nhân. Ca khám kết thúc.

---

## 3. CÁC LUỒNG QUẢN LÝ DANH MỤC (CATALOG FLOWS)

### 3.1 Luồng Quản lý Kho Thuốc (Medicine Management)
*   **Người dùng:** Admin / Dược sĩ.
*   **Hành động:** Thêm thuốc mới ➔ Cập nhật giá bán (`Price`) ➔ Cập nhật số lượng tồn kho gốc (`StockQuantity`).
*   **Lỗ hổng hiện tại (Gap):** Khi Bác sĩ kê đơn và lưu, số lượng thuốc trong kho (`StockQuantity`) hiện tại **chưa bị trừ đi (deduct)**. Cần một trigger hoặc logic trừ kho khi Hóa đơn được thanh toán hoặc khi Đơn thuốc được chốt.

### 3.2 Luồng Quản lý Dịch vụ & Bác sĩ (Service & Doctor Management)
*   **Người dùng:** Admin.
*   **Hành động:** Thêm các dịch vụ khám cận lâm sàng (Xét nghiệm máu, Siêu âm, X-Quang...) kèm giá tiền. Cập nhật danh sách Bác sĩ để Lễ tân có thể phân lịch.

---

## 4. LUỒNG THỐNG KÊ (DASHBOARD FLOW)
*   **Người dùng:** Chủ phòng khám / Admin.
*   **Hành động:** Truy cập tab "Trang Chủ" để xem biểu đồ bệnh nhân thăm khám trong tuần, tổng số bác sĩ, tổng doanh thu (cần bổ sung), và danh sách các ca khám gần nhất trong ngày để nắm bắt nhịp độ hoạt động.

---

## 5. NHỮNG ĐIỂM CẦN CẢI THIỆN ĐỂ HOÀN THIỆN NGHIỆP VỤ
Dựa trên phân tích luồng, dưới đây là các tính năng cần phát triển thêm để biến ClinicMS thành sản phẩm hoàn chỉnh:

1. **Trừ tồn kho thuốc tự động (Inventory Deduction):** Khi trạng thái Visit là Completed hoặc Invoice là Paid, tự động trừ `StockQuantity` của Medicine.
2. **Quản lý kết quả cận lâm sàng (Lab Results):** Bảng `ServiceOrder` đã có cột `Result`. Cần bổ sung UI để Bác sĩ hoặc Kỹ thuật viên nhập kết quả Siêu âm/Xét nghiệm.
3. **Lịch hẹn (Appointments):** Bảng `Visit` có `AppointmentId` nhưng chưa có hệ thống Đặt lịch hẹn trước (Booking).
4. **Phân quyền đăng nhập (Authentication & RBAC):** Thêm màn hình Đăng nhập. Ai là Bác sĩ thì chỉ thấy ca khám của mình, Thu ngân không được sửa chẩn đoán y khoa.