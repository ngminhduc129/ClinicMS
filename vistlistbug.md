# BÁO CÁO CHI TIẾT LỖI & PHƯƠNG ÁN KHẮC PHỤC: TAB HỒ SƠ KHÁM (VISIT LIST)

Tài liệu này phân tích các vấn đề kỹ thuật hiện có trong mã nguồn của tab Hồ sơ khám (`VisitListView`) và đề xuất giải pháp xử lý triệt để.

---

### 1. Lỗi Hiệu Năng Nghiêm Trọng (Performance Lag)
*   **Vị trí:** `VisitListView.xaml.cs`, phương thức `RebuildVisitList()`.
*   **Mô tả:** Hệ thống đang sử dụng code C# để xóa sạch và tạo mới hàng loạt các `Border`, `StackPanel` thủ công vào `ListBox` (`VisitGroupList`) mỗi khi có bất kỳ thay đổi nào (chọn mục, tìm kiếm, lọc ngày).
*   **Nguyên nhân kỹ thuật:** Việc tạo UI thủ công trong code-behind vô hiệu hóa tính năng **UI Virtualization** của WPF. Khi danh sách có >50 mục, CPU phải render lại hàng trăm đối tượng đồ họa không cần thiết trên UI Thread.
*   **Cách khắc phục:** 
    *   Chuyển sang dùng `DataBinding` thuần túy. 
    *   Sử dụng `CollectionViewSource` để nhóm (Grouping) dữ liệu theo Trạng thái ngay trong XAML.
    *   Định nghĩa `ItemTemplate` để WPF tự quản lý việc tái sử dụng (Recycle) các ô hiển thị.

### 2. Lỗi Đồng Bộ Số Liệu (Counters Out of Sync)
*   **Vị trí:** `MainViewModel.cs`, các hàm `AddVisitAsync`, `DeleteVisitAsync`.
*   **Mô tả:** Khi thêm hoặc xóa lượt khám thành công, danh sách hiển thị được cập nhật nhưng các ô thống kê (`TotalVisits`, `VisitsToday`) không thay đổi.
*   **Nguyên nhân kỹ thuật:** Các thuộc tính `TotalVisits` và `VisitsToday` chỉ được nạp một lần duy nhất khi khởi động ứng dụng (`LoadDashboardDataAsync`). Các hàm nghiệp vụ sau đó không gọi lại lệnh đếm từ DB.
*   **Cách khắc phục:** Sau mỗi thao tác Thay đổi (Add/Delete), cần gọi lại:
    ```csharp
    TotalVisits = await _db.GetVisitsCountAsync();
    VisitsToday = await _db.GetTodayVisitsCountAsync();
    ```

### 3. Lỗi Logic Nhận Diện Bệnh Nhân (Patient ID Resolution)
*   **Vị trí:** `VisitFormDialog.xaml.cs`, hàm `TryResolvePatientIdAsync()`.
*   **Mô tả:** Hệ thống tìm bệnh nhân để lưu hồ sơ bằng cách so khớp **Họ Tên** (`GetPatientByNameAsync`).
*   **Nguồn rủi ro:** Nếu hai bệnh nhân trùng tên, hệ thống sẽ luôn lưu hồ sơ vào người đầu tiên tìm thấy trong DB. Đây là lỗi nghiệp vụ y khoa nghiêm trọng.
*   **Cách khắc phục:** Cần lưu trữ `SelectedPatientId` ngay khi người dùng chọn từ danh sách gợi ý (Suggestion). Nếu người dùng tự gõ tên mới, yêu cầu tạo bệnh nhân mới trước thay vì tự suy diễn ID từ tên.

### 4. Lỗi Ổn Định Ứng Dụng (Async Void Anti-Pattern)
*   **Vị trí:** `VisitListView.xaml.cs` (hầu hết các sự kiện Click), `VisitFormDialog.xaml.cs` (Save_Click).
*   **Mô tả:** Sử dụng `async void` cho các tác vụ truy cập Database.
*   **Hậu quả:** Nếu Database bị khóa (Locked) hoặc có lỗi truy vấn, Exception sẽ không được bắt lại, dẫn đến ứng dụng bị **Crash (Đóng đột ngột)** mà không hiện thông báo lỗi.
*   **Cách khắc phục:** 
    *   Thay đổi toàn bộ `async void` thành `async Task` (nếu có thể) hoặc bao bọc toàn bộ nội dung trong khối `try-catch`.
    *   Sử dụng `ToastService.ShowError()` để thông báo lỗi cho người dùng thay vì để ứng dụng tự đóng.

### 5. Lỗi Bất Nhất Trạng Thái (Status Inconsistency)
*   **Vị trí:** `Models/Visit.cs` và logic trong `VisitListView.xaml.cs`.
*   **Mô tả:** Tồn tại sự nhầm lẫn giữa chuỗi `"In Progress"` (có dấu cách) và `"InProgress"` (không dấu cách) trong các hàm `NormalizeStatus` và `Save_Click`.
*   **Hậu quả:** Bộ lọc nhóm (Waiting/InProgress/Completed) hoạt động không chính xác, đôi khi không hiển thị đúng màu sắc cho lượt khám đang thực hiện.
*   **Cách khắc phục:** Chuẩn hóa một giá trị duy nhất trong Database (VD: `"In Progress"`) và sử dụng các hằng số (Constants) để so sánh trong code.

### 6. Lỗi Trải Nghiệm Người Dùng (UX Data Loss)
*   **Vị trí:** `VisitListView.xaml.cs`, sự kiện chọn mục trong danh sách.
*   **Mô tả:** Khi bác sĩ đang nhập chẩn đoán ở bảng bên phải nhưng chưa nhấn "Lưu", nếu họ nhấn vào một bệnh nhân khác ở danh sách bên trái, toàn bộ nội dung đang nhập sẽ **bị ghi đè và mất sạch**.
*   **Cách khắc phục:** Thêm cờ `IsDirty` để kiểm tra thay đổi. Nếu có thay đổi chưa lưu, hiển thị hộp thoại xác nhận: *"Bạn có thay đổi chưa lưu, vẫn muốn chuyển sang bệnh nhân khác?"*

---
*Báo cáo được khởi tạo bởi Gemini CLI - 2026*
