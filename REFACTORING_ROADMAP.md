# Lộ trình Tối ưu hóa và Sửa lỗi Dự án ClinicMS

Tài liệu này chi tiết các vấn đề kỹ thuật hiện tại và hướng dẫn cách khắc phục để đưa ứng dụng lên tiêu chuẩn sản xuất (Production-ready).

---

## 1. Lỗi Nghiêm trọng & Độ ổn định (Critical & Stability)

### ⚠️ Lỗi WPF Trigger (InvalidOperationException)
- **Vấn đề**: File error.txt ghi nhận lỗi: 'Text' property cannot be set from a property trigger and appear in the trigger's condition at the same time.
- **Nguyên nhân**: Một số Style của TextBlock hoặc TextBox đang dùng thuộc tính Text để vừa làm điều kiện kích hoạt (Trigger Condition), vừa làm giá trị thay đổi (Setter).
- **Cách sửa**: 
  - Kiểm tra lại các file XAML trong Resources\Themes\. 
  - Nếu muốn đổi Text dựa trên trạng thái, hãy dùng DataTrigger ràng buộc với một thuộc tính ViewModel hoặc dùng ControlTemplate riêng.
  - Tránh dùng Property="Text" bên trong <Trigger>.

### 🐛 Nuốt ngoại lệ (Silent Exceptions)
- **Vấn đề**: Trong MainViewModel.cs, nhiều khối catch { } đang để trống.
- **Cách sửa**: 
  - Thay bằng catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); }.
  - Sử dụng ILogger hoặc ghi log ra file để truy vết lỗi sau này.

---

## 2. Cấu trúc Dữ liệu (Data Integrity)

### 📅 Chuẩn hóa Ngày tháng (DateTime Handling)
- **Vấn đề**: VisitTime, CreatedAt đang lưu dạng string trong SQLite.
- **Cách sửa**:
  1. Thay đổi kiểu dữ liệu trong Models sang DateTime.
  2. Cấu hình EF Core để tự động chuyển đổi (Value Converters) hoặc đổi kiểu cột trong DB sang DATETIME.
  3. Sử dụng định dạng ISO 8601 (yyyy-MM-dd HH:mm:ss) để đảm bảo khả năng sắp xếp và lọc trong SQL.

### 🛡️ Xác thực dữ liệu (Data Validation)
- **Vấn đề**: Chưa có ràng buộc dữ liệu đầu vào (ví dụ: số điện thoại trống, định dạng email sai).
- **Cách sửa**:
  - Triển khai giao diện INotifyDataErrorInfo trong BaseViewModel hoặc các lớp Model.
  - Sử dụng DataAnnotations (như [Required], [Phone]) để tự động kiểm tra lỗi trên giao diện.

---

## 3. Kiến trúc mã nguồn (Refactoring God Object)

### 🧩 Phân tách MainViewModel
- **Vấn đề**: MainViewModel đang chứa logic của 5 thực thể khác nhau, vi phạm nguyên tắc Đơn nhiệm (Single Responsibility).
- **Cách sửa**:
  - Tạo các ViewModel riêng biệt: PatientViewModel, DoctorViewModel, InventoryViewModel, InvoiceViewModel.
  - MainViewModel chỉ quản lý việc điều hướng (Navigation) và chứa các ViewModel con này.

### 💉 Dependency Injection (DI)
- **Vấn đề**: DatabaseService và ToastService đang được khởi tạo thủ công bằng từ khóa 
ew.
- **Cách sửa**:
  - Sử dụng thư viện Microsoft.Extensions.DependencyInjection.
  - Đăng ký Services trong App.xaml.cs và truyền vào các ViewModel qua hàm tạo (Constructor Injection).

---

## 4. Giao diện người dùng (UI Consistency)

### 🎨 Đồng bộ hóa Dashboard & List Views
- **Vấn đề**: PatientListView.xaml đã đẹp nhưng DoctorListView.xaml và các trang khác vẫn dùng code cũ hoặc chưa tối ưu DataGrid.
- **Cách sửa**:
  - Áp dụng Style="{StaticResource CardContainerStyle}" cho tất cả các bảng dữ liệu.
  - Chuẩn hóa chiều cao dòng DataGrid về 56px.
  - Thay thế các màu cứng (hard-coded colors) bằng các StaticResource đã định nghĩa trong ThemeColors.xaml.

---

## 5. Bảo mật & Bảo trì (Security & Maintenance)

### 🔐 Cập nhật thư viện
- **Vấn đề**: Microsoft.Extensions.Caching.Memory 8.0.0 có lỗ hổng bảo mật.
- **Cách sửa**: Chỉnh sửa file .csproj, nâng cấp phiên bản lên 8.0.1 hoặc mới hơn.

### 🧹 Dọn dẹp thư mục rác
- **Vấn đề**: Thư mục tên {Models,ViewModels,Services,Views,Converters,Resources} gây nhầm lẫn.
- **Cách sửa**: Xóa bỏ thư mục này ngay lập tức.

### 🧪 Bổ sung Kiểm thử (Unit Testing)
- **Vấn đề**: Chưa có kiểm thử tự động.
- **Cách sửa**: 
  - Tạo một project ClinicMS.Tests sử dụng xUnit.
  - Viết test cho DatabaseService để đảm bảo logic thêm/xóa/sửa bệnh nhân không làm hỏng dữ liệu liên kết (Contacts).

---
