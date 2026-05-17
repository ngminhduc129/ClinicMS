# KẾ HOẠCH TRIỂN KHAI TAB HÓA ĐƠN (INVOICE MANAGEMENT)

## 1. Mục Tiêu (Objective)
Xây dựng hoàn chỉnh tính năng Quản lý Hóa Đơn (Tab Hóa Đơn) dựa trên kiến trúc MVVM hiện tại. Tính năng này cho phép phòng khám tự động xuất hóa đơn từ hồ sơ khám, theo dõi công nợ, nhận thanh toán và in biên lai.

## 2. Phân Tích Hiện Trạng
- **Đã có:** Các Model `Invoice`, `InvoiceItem`, `Payment`. Cơ sở dữ liệu đã sẵn sàng. `InvoiceService` đã có các hàm cơ bản (Get, UpdateStatus, AddPayment). `PdfExportService` đã hỗ trợ in Hóa Đơn.
- **Còn thiếu:** Thiếu logic tự động tổng hợp tiền (từ Đơn thuốc và Dịch vụ), chưa có `InvoiceViewModel` và giao diện `InvoiceListView`.

## 3. Các Tính Năng Cốt Lõi Cần Xây Dựng
1. **Tự động lập hóa đơn:** Khi bác sĩ kê thuốc hoặc chỉ định dịch vụ, hệ thống tự động gom nhóm lại thành một Hóa đơn cho ca khám đó.
2. **Giao diện Master-Detail:** Tương tự tab Hồ sơ khám, bên trái là danh sách Hóa đơn (phân nhóm: Chưa thanh toán, Đã thanh toán 1 phần, Đã thanh toán), bên phải là Chi tiết hóa đơn (Thông tin bệnh nhân, danh sách các khoản phí).
3. **Chức năng Thanh toán (Payment Flow):** Hỗ trợ thanh toán nhiều lần cho một hóa đơn. Có form nhập số tiền thanh toán, chọn phương thức (Tiền mặt/Chuyển khoản).
4. **In biên lai (Export PDF):** Tích hợp nút in hóa đơn sử dụng thư viện PDF hiện có.

## 4. Các Bước Triển Khai (Phased Implementation)

### Bước 1: Nâng cấp Dịch vụ (Service & ViewModel Layer)
- **`InvoiceService.cs`**: Viết thêm hàm `GenerateOrUpdateInvoiceAsync(int visitId)`. Hàm này sẽ đọc toàn bộ Đơn thuốc (`PrescriptionDetail`) và Dịch vụ (`ServiceOrder`) của một `Visit`, tính toán `TotalAmount` và lưu vào `InvoiceItem` / `Invoice`.
- **`InvoiceViewModel.cs`**: Tạo mới ViewModel. Quản lý `ObservableCollection<Invoice>`, logic lọc (Filter) theo trạng thái, và các Command như `PayCommand`, `PrintCommand`, `LoadAsync`.

### Bước 2: Xây dựng Giao diện (View Layer)
- **`PaymentDialog.xaml`**: Tạo một Dialog (Popup) nhỏ để thu ngân nhập số tiền thanh toán, hiển thị số tiền còn nợ, và chọn phương thức thanh toán.
- **`InvoiceListView.xaml`**: 
  - Cột trái: DataGrid/ListView danh sách Hóa đơn, có Grouping theo `Status` (Màu đỏ: Chưa thanh toán, Xanh: Đã thanh toán).
  - Cột phải: Panel Chi tiết hiển thị tổng tiền, danh sách `InvoiceItem` (Tiền thuốc, tiền dịch vụ), và lịch sử `Payments`.

### Bước 3: Tích hợp Hệ thống (Integration)
- Đăng ký `InvoiceViewModel` vào **`MainViewModel.cs`** (Dependency Injection).
- Sửa **`MainWindow.xaml`**: Thay thế dòng code giữ chỗ `[I] Quản Lý Hóa Đơn (Coming Soon)` bằng Component `<views:InvoiceListView/>`.
- Bổ sung logic tự động: Tích hợp hàm `GenerateOrUpdateInvoiceAsync` vào `VisitListView.xaml.cs` (khi nhấn Lưu Bệnh Án thì hóa đơn cũng được cập nhật theo).

## 5. Kiểm thử & Xác nhận (Verification)
- Tạo một ca khám mới, kê thuốc, chỉ định dịch vụ.
- Chuyển sang Tab Hóa Đơn, kiểm tra xem hệ thống có tự sinh hóa đơn với tổng tiền chính xác không.
- Tiến hành thanh toán một phần -> Hóa đơn chuyển trạng thái "Partial".
- Thanh toán phần còn lại -> Hóa đơn chuyển trạng thái "Paid".
- Nhấn nút "In hóa đơn" để xuất file PDF và kiểm tra layout.

---
*Bản kế hoạch này được lập tự động bởi Gemini CLI - 2026*