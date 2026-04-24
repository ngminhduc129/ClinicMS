# Kế Hoạch Chi Tiết: Nâng Cấp & Tối Ưu Hóa Hệ Thống ClinicMS (Phiên bản Toàn diện)

Tài liệu này cung cấp lộ trình kỹ thuật chi tiết để chuyển đổi ClinicMS từ một ứng dụng prototype thành một sản phẩm phần mềm chuyên nghiệp, tuân thủ các tiêu chuẩn công nghiệp của hệ sinh thái .NET/WPF.

---

## 1. Tái Cấu Trúc Hệ Thống (Core Architecture)

### 1.1. Triển khai Dependency Injection (DI) & IoC Container
*   **Vấn đề hiện tại:** Các class tự khởi tạo phụ thuộc của mình (ví dụ: `new DatabaseService()`), gây khó khăn cho việc mở rộng và kiểm thử tự động.
*   **Giải pháp:** Sử dụng `Microsoft.Extensions.DependencyInjection`.
*   **Chi tiết thực hiện:**
    1.  **Cài đặt:** Thêm NuGet package `Microsoft.Extensions.DependencyInjection`.
    2.  **Khởi tạo tại App.xaml.cs:**
        - Tạo một property `IServiceProvider`.
        - Trong `OnStartup`, khởi tạo `ServiceCollection`, đăng ký các dịch vụ:
            - `services.AddDbContext<ClinicDbContext>()` (Scoped)
            - `services.AddSingleton<DatabaseService>()`
            - `services.AddSingleton<ToastService>()`
            - `services.AddSingleton<ModalService>()`
            - `services.AddTransient<MainViewModel>()`
            - `services.AddTransient<PatientViewModel>()` (mới)
    3.  **Sử dụng:** Chuyển sang dùng Constructor Injection trong các ViewModel và Dialog.

### 1.2. Phân rã "God ViewModel" (MainViewModel Refactoring)
*   **Vấn đề hiện tại:** `MainViewModel.cs` dài hàng nghìn dòng, quản lý mọi thứ từ Dashboard đến Hóa đơn.
*   **Giải pháp:** Áp dụng mô hình Sub-ViewModel.
*   **Chi tiết thực hiện:**
    1.  **Tạo các ViewModel chuyên biệt:**
        - `DashboardViewModel.cs`: Thống kê, biểu đồ tuần, danh sách khám hôm nay.
        - `PatientManagementViewModel.cs`: Danh sách bệnh nhân, tìm kiếm, phân trang.
        - `VisitManagementViewModel.cs`: Danh sách hồ sơ khám, bộ lọc ngày.
        - `InventoryViewModel.cs`: Quản lý kho thuốc.
    2.  **Cơ chế Điều hướng (Navigation):**
        - Trong `MainViewModel`, dùng một thuộc tính `CurrentViewModel` kiểu `BaseViewModel`.
        - Trong `MainWindow.xaml`, dùng `ContentControl` với `DataTemplate` để tự động ánh xạ ViewModel sang View tương ứng.

### 1.3. Chuẩn hóa đường dẫn Database (Environment-Aware Paths)
*   **Vấn đề hiện tại:** Đường dẫn ổ `D:\` gây lỗi khi chạy trên máy tính khách.
*   **Chi tiết thực hiện:**
    1.  **Logic động trong `ClinicDbContext.cs`:**
        ```csharp
        string dbPath;
        if (Debugger.IsAttached) {
            // Chế độ Dev: Dùng file ngay trong thư mục dự án
            dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clinic_database.db");
        } else {
            // Chế độ Production: Dùng AppData
            dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClinicMS", "clinic.db");
        }
        ```
    2.  **Cấu hình:** Sử dụng `appsettings.json` để cho phép người dùng nâng cao tự đổi đường dẫn nếu cần.

---

## 2. Nâng Cấp Giao Diện & Trải Nghiệm (UI/UX)

### 2.1. Chuyển đổi sang DataTemplate cho Danh sách động
*   **Vấn đề hiện tại:** Dùng code-behind (`MakeMedicineRow`) để vẽ UI cực kỳ khó bảo trì và không thể áp dụng Style chung.
*   **Chi tiết thực hiện:**
    1.  **Định nghĩa trong Resources:** Tạo `DataTemplate` cho `PrescriptionDetail` trong `ClinicTheme.xaml`.
    2.  **XAML Implementation:**
        ```xml
        <ItemsControl ItemsSource="{Binding PrescriptionDetails}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Grid Margin="0,5">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="80" />
                            <ColumnDefinition Width="150" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="{Binding Medicine.MedicineName}" />
                        <TextBlock Grid.Column="1" Text="{Binding Quantity}" />
                        <TextBlock Grid.Column="2" Text="{Binding Dosage}" />
                    </Grid>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        ```

### 2.2. Triển khai Hệ thống Ràng buộc (Validation System)
*   **Mục tiêu:** Không cho phép nhấn "Lưu" nếu dữ liệu không hợp lệ.
*   **Chi tiết thực hiện:**
    1.  **Base Class:** Thêm logic xử lý lỗi vào `BaseViewModel.cs` thông qua `INotifyDataErrorInfo`.
    2.  **Quy tắc kiểm tra (Rules):**
        - Bệnh nhân: Tên (bắt buộc), Số điện thoại (10 số), Năm sinh (hợp lệ).
        - Khám bệnh: Chẩn đoán (không để trống nếu trạng thái là Completed).
    3.  **UI Feedback:** Hiển thị viền đỏ và tooltip thông báo lỗi ngay tại TextBox.

---

## 3. Quản trị Dữ liệu & An toàn (Data Management)

### 3.1. Chuyển đổi sang EF Core Migrations
*   **Vấn đề hiện tại:** Tạo bảng bằng SQL thủ công dễ gây lỗi "Schema Mismatch".
*   **Chi tiết thực hiện:**
    1.  **Setup:** Cài đặt `Microsoft.EntityFrameworkCore.Tools`.
    2.  **Khởi tạo:** Chạy lệnh `dotnet ef migrations add InitialCreate`.
    3.  **Cập nhật:** Thay `db.Database.EnsureCreated()` bằng `db.Database.Migrate()` trong `DatabaseService.Initialize()`. Điều này đảm bảo khi bạn thêm cột mới ở Model, DB của người dùng sẽ tự cập nhật mà không mất dữ liệu cũ.

### 3.2. Hệ thống Logging & Global Error Handling
*   **Mục tiêu:** "Biết mọi thứ" xảy ra khi ứng dụng lỗi ở máy khách.
*   **Chi tiết thực hiện:**
    1.  **Serilog:** Cài đặt Serilog để ghi log vào file hàng ngày (`logs/log-.txt`).
    2.  **App.xaml.cs Exception Catching:**
        ```csharp
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            Log.Fatal(e.Exception, "Ứng dụng bị dừng đột ngột!");
            MessageBox.Show("Đã có lỗi nghiêm trọng xảy ra. Vui lòng kiểm tra file log.");
            e.Handled = true;
        }
        ```

---

## 4. Kiểm thử & Đảm bảo chất lượng (Quality Assurance)

### 4.1. Unit Testing cho Logic nghiệp vụ
*   **Chi tiết thực hiện:**
    - Sử dụng `xUnit` và `Moq`.
    - Viết test cho `DatabaseService`: Kiểm tra tính toán hóa đơn, kiểm tra lưu đơn thuốc có đúng số lượng không.
    - Viết test cho các `Validators`.

---

## Lộ trình thực hiện khuyến nghị

| Giai đoạn | Nhiệm vụ trọng tâm | Thời gian dự kiến |
|:---:|:---|:---:|
| **1** | Sửa Hardcoded Paths + Logging + Global Catch (Ổn định nền tảng) | 1 ngày |
| **2** | Triển khai DI + Tách nhỏ 2 ViewModels chính (Cấu trúc lại) | 2 ngày |
| **3** | Chuyển sang DataTemplate cho Visit & Medicine (Làm sạch UI code) | 1.5 ngày |
| **4** | Validation & EF Migrations (Hoàn thiện nghiệp vụ) | 1.5 ngày |

---
*Người lập kế hoạch: Gemini CLI*
*Ngày cập nhật: 23/04/2026*
