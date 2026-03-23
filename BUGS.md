# DANH SÁCH LỖI DỰ ÁN (BUGS.md)

Tài liệu này tổng hợp các lỗi logic, hiệu năng và giao diện được phát hiện sau khi rà soát toàn bộ mã nguồn dự án ClinicMS.

---

## 1. LỖI NGHIÊM TRỌNG (GÂY TREO ỨNG DỤNG - DEADLOCK)
### **File: `Views\VisitFormDialog.xaml.cs`**
*   **Vấn đề:** Sử dụng `.Result` và `.Wait()` trên các phương thức bất đồng bộ (Async) ngay tại luồng giao diện (UI Thread).
*   **Mô tả:** Trong các hàm `TryResolvePatientId` và `TryResolveDoctorId`, mã nguồn gọi `_vm.GetPatientByNameAsync(...).Result`. Khi chạy trên WPF, việc này sẽ khóa luồng chính để chờ kết quả, nhưng Task lại cần luồng chính để hoàn thành -> Gây treo ứng dụng (Deadlock).
*   **Hệ quả:** Ứng dụng bị "đơ" hoàn toàn khi nhấn nút Lưu hoặc khi tìm kiếm tên bệnh nhân/bác sĩ.

---

## 2. LỖI HIỆU NĂNG (GÂY LAG CỰC NẶNG)
### **File: `Views\VisitListView.xaml.cs`**
*   **Vấn đề:** Tự vẽ UI thủ công (Manual UI Rendering) thay vì dùng Data Binding.
*   **Mô tả:** Hàm `RebuildVisitList` sử dụng `Children.Clear()` và sau đó dùng vòng lặp `foreach` để tạo hàng trăm đối tượng `Border`, `Grid`, `TextBlock` bằng code-behind.
*   **Hệ quả:** 
    - Bỏ qua tính năng **UI Virtualization** của WPF (chỉ vẽ những gì hiện trên màn hình). 
    - Mỗi khi lọc dữ liệu hoặc cuộn trang, CPU phải tính toán lại toàn bộ cây thư mục UI, gây hiện tượng giật lag, tiêu tốn rất nhiều RAM.
    - Càng nhiều hồ sơ khám, ứng dụng càng chậm.

---

## 3. LỖI LOGIC & DỮ LIỆU
### **File: `ViewModels\MainViewModel.cs` & `DatabaseService.cs`**
*   **Vấn đề:** Thiếu logic lưu đơn thuốc (Prescription Details).
*   **Mô tả:** Trong `VisitFormDialog`, người dùng có thể thêm danh sách thuốc vào `_prescriptionItems`, nhưng hàm `Save_Click` hiện tại mới chỉ lưu thông tin cơ bản của `Visit` (Triệu chứng, chẩn đoán) mà chưa lưu danh sách thuốc xuống bảng `prescription_details`.
*   **Hệ quả:** Đơn thuốc bị mất sau khi đóng form.
*   **Vấn đề khác:** Trong `AddPatientAsync`, các trường `Phone`, `Email`, `Address` trong đối tượng `Patient` bị gán bằng `null` ngay trong bộ nhớ sau khi lưu vào bảng `Contacts`. Nếu đối tượng này tiếp tục được sử dụng ở nơi khác, thông tin liên hệ sẽ bị trống.

---

## 4. LỖI UI/UX (NÚT BẤM KHÔNG PHẢN HỒI)
### **File: Toàn bộ dự án (Views)**
*   **Vấn đề:** Sử dụng `async void` không có xử lý lỗi (`try-catch`).
*   **Mô tả:** Hầu hết các sự kiện `Click` đều là `async void`. Nếu có lỗi xảy ra bên trong (ví dụ lỗi Database), ứng dụng sẽ không báo gì cả (người dùng ấn nút nhưng không thấy phản hồi) hoặc bị sập ứng dụng (Crash) ngay lập tức mà không có thông báo lỗi cụ thể.
*   **Hệ quả:** Người dùng cảm thấy ứng dụng bị hỏng hoặc nút bấm không hoạt động.

---

## 5. LỖI LOAD DỮ LIỆU
### **File: `Views\PatientListView.xaml.cs`**
*   **Vấn đề:** Vòng lặp sự kiện vô tận (Infinite Event Loop).
*   **Mô tả:** Sự kiện `OnPatientsCollectionChanged` gọi `ApplyFilter`, bên trong `ApplyFilter` lại có thể gọi `LoadPatientsAsync`, việc nạp lại danh sách lại kích hoạt sự kiện `CollectionChanged` -> Vòng lặp.
*   **Hệ quả:** Danh sách bệnh nhân có thể bị trắng hoặc nhảy liên tục, không hiển thị đúng bệnh nhân vừa thêm. (Đã được xử lý một phần, nhưng cần kiểm tra ở các ListView khác).

---

## KHUYẾN NGHỊ SỬA LỖI:
1.  **Threading:** Chuyển tất cả `.Result` / `.Wait()` sang `await`.
2.  **Performance:** Thay thế việc vẽ UI thủ công bằng `ItemsControl` hoặc `DataGrid` kết hợp với `ObservableCollection`.
3.  **Dữ liệu:** Bổ sung Transaction khi lưu `Visit` cùng với `PrescriptionDetails` để đảm bảo dữ liệu đồng nhất.
4.  **UI:** Thêm `ToastService.ShowError` trong khối `catch` của các hàm `async void`.
