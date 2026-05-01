# Tài Liệu Kỹ Thuật Dự Án ClinicMS

ClinicMS là một ứng dụng quản lý phòng khám hiện đại được phát triển trên nền tảng WPF (Windows Presentation Foundation) với ngôn ngữ C# và .NET 8. Ứng dụng được thiết kế theo mô hình MVVM (Model-View-ViewModel) giúp tách biệt logic nghiệp vụ và giao diện người dùng, đảm bảo khả năng bảo trì và mở rộng cao.

---

## 1. Tổng Quan Công Nghệ (Tech Stack)

*   **Framework:** .NET 8.0 Windows (WPF)
*   **Ngôn ngữ:** C# 12
*   **Cơ sở dữ liệu:** SQLite
*   **ORM:** Entity Framework Core 8.0
*   **Logging:** Serilog (Ghi log vào file theo ngày)
*   **Dependency Injection:** Microsoft.Extensions.DependencyInjection
*   **Kiến trúc:** MVVM (Model-View-ViewModel)

---

## 2. Cấu Trúc Thư Mục

```text
ClinicMS/
├── Data/               # Lớp truy cập dữ liệu (EF Core DbContext)
├── Models/             # Các thực thể dữ liệu (Entities)
├── ViewModels/         # Logic điều khiển giao diện (MVVM ViewModels)
├── Views/              # Giao diện người dùng (XAML & Code-behind)
├── Services/           # Các dịch vụ dùng chung (Database, Toast, Modal, FontScale)
├── Converters/         # Các bộ chuyển đổi dữ liệu XAML (Binding Converters)
├── Resources/          # Tài nguyên ứng dụng (Icons, Themes, Styles)
├── ClinicMS.Tests/     # Dự án kiểm thử (Unit Tests)
└── Logs/               # Thư mục lưu trữ log (được tạo trong AppData)
```

---

## 3. Kiến Trúc Hệ Thống

### 3.1. Quản lý Vòng đời Ứng dụng (`App.xaml.cs`)
Ứng dụng sử dụng `IServiceCollection` để đăng ký các dịch vụ ngay khi khởi động:
*   **Singleton Services:** `DatabaseService`, `ToastService`, `ModalService`, `MainViewModel`.
*   **Transient ViewModels:** Các ViewModel con như `DashboardViewModel`, `PatientViewModel`, v.v. được khởi tạo mới khi cần.
*   **Localization:** Tự động ép định dạng văn hóa Việt Nam (`vi-VN`) cho ngày tháng và tiền tệ.
*   **Global Exception Handling:** Xử lý lỗi tập trung trên UI Thread, AppDomain và Task chạy ngầm.

### 3.2. Lớp Dữ Liệu (`DatabaseService.cs` & `ClinicDbContext.cs`)
Hệ thống sử dụng SQLite làm cơ sở dữ liệu cục bộ:
*   **Tự động khởi tạo:** `DatabaseService.Initialize()` kiểm tra và tạo bảng nếu chưa tồn tại, hỗ trợ cập nhật Schema qua SQL Raw.
*   **Quản lý file DB:** File `clinic_database.db` được ưu tiên lưu trong thư mục `AppData\Local\ClinicMS` khi chạy bản Release để đảm bảo không bị mất dữ liệu khi cập nhật ứng dụng.
*   **Truy vấn:** Sử dụng LINQ to Entities kết hợp với `Include` để tải dữ liệu liên quan (Eager Loading).

### 3.3. Điều phối Giao diện (`MainViewModel.cs`)
`MainViewModel` đóng vai trò là "nhà điều phối" chính:
*   Quản lý trạng thái chuyển trang (`CurrentPage`).
*   Quản lý tỷ lệ phóng to giao diện (`FontScale`).
*   Cung cấp các lệnh điều hướng (`NavigateCommand`).

---

## 4. Mô Hình Dữ Liệu (Entities)

Các thực thể chính bao gồm:
*   **Patient (Bệnh nhân):** Thông tin cá nhân, tiểu sử, chỉ số cơ thể.
*   **Doctor (Bác sĩ):** Thông tin chuyên môn.
*   **Visit (Hồ sơ khám):** Lưu lại lịch sử khám bệnh, triệu chứng, chẩn đoán, và đơn thuốc.
*   **Appointment (Lịch hẹn):** Quản lý lịch hẹn giữa bệnh nhân và bác sĩ.
*   **Medicine (Thuốc):** Danh mục thuốc, đơn giá và số lượng tồn kho.
*   **Prescription Detail (Chi tiết đơn thuốc):** Liên kết giữa hồ sơ khám và thuốc.
*   **Invoice & Payment (Hóa đơn & Thanh toán):** Quản lý tài chính cho mỗi lượt khám.

---

## 5. Các Tính Năng Nổi Bật

### 5.1. Giao diện Hiện đại & Tùy biến (Theming)
*   Sử dụng hệ thống tài nguyên XAML tách biệt (`ThemeColors.xaml`, `ThemeControls.xaml`, ...) giúp dễ dàng thay đổi giao diện.
*   **Font Scaling:** Cho phép người dùng chọn mức độ hiển thị (Nhỏ, Vừa, Lớn) để phù hợp với độ phân giải màn hình hoặc thị lực.

### 5.2. Hệ thống Thông báo (Toast) & Hội thoại (Modal)
*   **ToastService:** Hiển thị thông báo nhanh ở góc màn hình mà không làm gián đoạn công việc.
*   **ModalService:** Quản lý việc hiển thị các hộp thoại (dialog) như Form nhập liệu bệnh nhân, bác sĩ, đơn thuốc ngay trên lớp Overlay của cửa sổ chính.

### 5.3. Dashboard Trực quan
*   Thống kê nhanh số lượng bệnh nhân, lượt khám trong ngày/tháng.
*   Danh sách lịch hẹn sắp tới.
*   Biểu đồ xu hướng lượt khám bằng hình khối đơn giản, hiệu quả.

---

## 6. Hướng Dẫn Phát Triển

### 6.1. Cài đặt Môi trường
1.  Cài đặt **Visual Studio 2022** (bản 17.8 trở lên) hoặc **JetBrains Rider**.
2.  Cài đặt workload **.NET Desktop Development**.
3.  Mở file `ClinicMS.sln`.

### 6.2. Cập nhật Cơ sở dữ liệu
*   Nếu thay đổi Model, hãy cập nhật `ClinicDbContext`.
*   Sử dụng `DatabaseService.Initialize()` hoặc EF Core Migrations (nếu cấu hình) để đồng bộ DB.

### 6.3. Kiểm thử (Testing)
*   Dự án `ClinicMS.Tests` sử dụng xUnit để kiểm thử logic trong `DatabaseService` và các ViewModel quan trọng.

---

## 7. Kết Luận
ClinicMS là một giải pháp quản lý phòng khám nhỏ gọn nhưng đầy đủ tính năng, tập trung vào trải nghiệm người dùng mượt mà và tính ổn định của dữ liệu. Cấu trúc MVVM sạch sẽ giúp ứng dụng dễ dàng mở rộng thêm các module như Quản lý Xét nghiệm, Báo cáo Tài chính chuyên sâu trong tương lai.
