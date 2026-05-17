# KẾ HOẠCH TRIỂN KHAI TAB DỊCH VỤ (SERVICES)

## 1. Mục Tiêu (Objective)
Bổ sung tính năng quản lý Danh mục Dịch vụ (khám bệnh, xét nghiệm, siêu âm, v.v.) vào hệ thống. Cung cấp giao diện để người dùng có thể Xem, Thêm, Sửa, Xóa các dịch vụ và đơn giá của chúng.

## 2. Bối Cảnh (Key Files & Context)
- **Database:** Đã có sẵn model `Service` (`service_id`, `service_name`, `price`) và bảng `services` trong SQLite.
- **Service Layer:** `DatabaseService.cs` đã tích hợp sẵn các hàm `GetServicesAsync`, `AddServiceAsync`, `UpdateServiceAsync`, `DeleteServiceAsync`.
- **Thành phần cần xây dựng mới:**
  - `ViewModels/ServiceViewModel.cs`
  - `Views/ServiceListView.xaml` và `Views/ServiceListView.xaml.cs`
  - `Views/ServiceFormDialog.xaml` và `Views/ServiceFormDialog.xaml.cs`
- **Thành phần cần cập nhật:**
  - `App.xaml.cs` (Đăng ký DI cho `ServiceViewModel`)
  - `ViewModels/MainViewModel.cs` (Quản lý trạng thái trang và điều hướng)
  - `Views/MainWindow.xaml` (Thêm nút điều hướng ở Sidebar)

## 3. Các Bước Thực Hiện (Implementation Steps)

### Bước 1: Tạo ViewModels
- Tạo file `ServiceViewModel.cs` kế thừa từ `BaseViewModel`.
- Thêm `ObservableCollection<Service> Services` và danh sách phụ để tìm kiếm `FilteredServices`.
- Viết các phương thức: `LoadAsync()`, `SearchAsync(string term)`, `AddAsync(Service s)`, `UpdateAsync(Service s)`, `DeleteAsync(Service s)`.

### Bước 2: Cập Nhật MainViewModel & Dependency Injection
- Mở `App.xaml.cs` và thêm `services.AddTransient<ServiceViewModel>();`.
- Mở `MainViewModel.cs`:
  - Thêm property `ServiceViewModel ServiceVM { get; }`.
  - Nhận `ServiceViewModel` thông qua constructor.
  - Cập nhật property `CurrentPage` để gọi `ServiceVM.LoadAsync()` khi trang chuyển sang `"services"`.
  - Thêm property `public bool IsPageServices => CurrentPage == "services";`.

### Bước 3: Thêm Điều Hướng Tại Giao Diện Chính
- Mở `MainWindow.xaml` và thêm một `NavButton` mới cho "Dịch vụ" với `PageKey="services"`.
- Liên kết nó với ContentControl để hiển thị `ServiceListView` tương ứng thông qua `DataTrigger`.

### Bước 4: Tạo Giao Diện Quản Lý (ServiceListView)
- Thiết kế `ServiceListView.xaml` gồm:
  - Header: Thống kê tổng số dịch vụ, Nút Làm mới, Nút Thêm mới.
  - Thanh tìm kiếm (Search Bar) sử dụng Timer (Debounce) để lọc danh sách.
  - DataGrid / ListView hiển thị danh sách các dịch vụ (`Tên dịch vụ`, `Giá tiền`, `Nút Sửa`, `Nút Xóa`).
- Cấu hình code-behind `ServiceListView.xaml.cs` để nối kết sự kiện với các command.

### Bước 5: Tạo Giao Diện Thêm/Sửa (ServiceFormDialog)
- Thiết kế `ServiceFormDialog.xaml` là một UserControl dạng Modal.
- Cung cấp TextBox cho `ServiceName` và TextBox nhập liệu hỗ trợ số cho `Price`.
- Code-behind xử lý validation: Bắt buộc nhập tên dịch vụ, giá tiền phải là số nguyên dương.

## 4. Xác Minh và Kiểm Thử (Verification & Testing)
- **Tích hợp:** Ứng dụng build thành công không có lỗi biên dịch sau khi thêm DI.
- **Chức năng:** Thêm, Sửa, Xóa dịch vụ đều được lưu chính xác xuống Database.
- **Hiển thị:** Chuyển đổi tab bằng nút ở Sidebar hoạt động đúng, giao diện cập nhật ngay lập tức. Tính năng tìm kiếm debounce trả về kết quả mượt mà.
