# BÁO CÁO CHI TIẾT LỖI HỆ THỐNG & TRẠNG THÁI KHẮC PHỤC (BUG.MD)

Tài liệu này ghi nhận toàn bộ các lỗi giao diện, hiệu năng, kiến trúc, và cơ sở dữ liệu đã từng xảy ra trong hệ thống **ClinicMS**, kèm theo trạng thái xử lý và giải pháp kỹ thuật cụ thể đã áp dụng.

---

## 1. PHÂN NHÓM LỖI GIAO DIỆN & HIỆU NĂNG (UI/UX & PERFORMANCE)

### 1.1. Vẽ giao diện thủ công gây giật lag (UI Recreation)
*   **Vị trí:** `VisitListView.xaml.cs` (phương thức `RebuildVisitList` cũ).
*   **Mô tả:** Hệ thống cũ xóa sạch và tạo mới hàng loạt các `Border`, `StackPanel` thủ công trong code-behind mỗi khi người dùng thay đổi bộ lọc hoặc chọn ca khám, gây đơ luồng giao diện chính (UI Thread).
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:**
    *   Chuyển sang dùng cơ chế liên kết dữ liệu (**Data Binding**) thuần túy của WPF.
    *   Sử dụng `CollectionViewSource` để thực hiện Gom nhóm (Grouping) và Sắp xếp (Sorting) trực tiếp trong mã XAML.
    *   Tận dụng tính năng **UI Virtualization** của WPF để tái sử dụng phần tử đồ họa khi cuộn danh sách lớn.

### 1.2. Số liệu thống kê ở Dashboard không tự cập nhật (Counters Out of Sync)
*   **Vị trí:** `MainViewModel.cs` phối hợp với các ViewModels con.
*   **Mô tả:** Khi thêm hoặc xóa ca khám/bệnh nhân thành công, các con số đếm thống kê tổng trên màn hình trang chủ (Dashboard) vẫn giữ nguyên giá trị cũ, chỉ cập nhật khi khởi động lại ứng dụng.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:**
    *   Khai báo sự kiện `DataChanged` dạng `event Action` trong [VisitViewModel.cs](file:///e:/ClinicMS/ViewModels/VisitViewModel.cs) và các ViewModel nghiệp vụ khác.
    *   Đăng ký lắng nghe sự kiện này tại [MainViewModel.cs](file:///e:/ClinicMS/ViewModels/MainViewModel.cs). Khi phát hiện thay đổi dữ liệu, hệ thống tự động gọi lệnh truy vấn cập nhật `DashboardVM.LoadAsync()`.

### 1.3. Nguy cơ mất dữ liệu đang nhập dở (UX Data Loss)
*   **Vị trí:** `VisitListView.xaml.cs` (Sự kiện chọn dòng trong danh sách ca khám).
*   **Mô tả:** Khi bác sĩ đang nhập chẩn đoán hoặc kê thuốc cho một ca bệnh nhưng chưa nhấn nút "Lưu", nếu vô tình click chọn một bệnh nhân khác ở danh sách bên trái, toàn bộ nội dung đang gõ dở lập tức bị ghi đè và biến mất.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:**
    *   Thêm cờ giám sát trạng thái `_isDirty` (chuyển sang `true` khi người dùng gõ phím hoặc thay đổi trạng thái).
    *   Khi sự kiện `SelectionChanged` kích hoạt, nếu phát hiện `_isDirty == true`, hệ thống sẽ chặn hành động chuyển dòng và hiển thị hộp thoại xác nhận [ConfirmDialog](file:///e:/ClinicMS/Views/ConfirmDialog.xaml). Nếu người dùng chọn không chuyển, ứng dụng tự động hoàn trả vùng chọn cũ.

---

## 2. PHÂN NHÓM LỖI LOGIC NGHIỆP VỤ & ORM (BUSINESS LOGIC & DATABASE)

### 2.1. Nhận diện ID bệnh nhân bằng so khớp chuỗi Họ Tên (Patient ID Collision)
*   **Vị trí:** `VisitFormDialog.xaml.cs` (hàm `TryResolvePatientIdAsync`).
*   **Mô tả:** Hệ thống phân tích ID bệnh nhân bằng cách so khớp chính xác chuỗi họ tên người dùng nhập. Khi phòng khám có hai bệnh nhân trùng tên, ca khám luôn bị gán sai cho người đầu tiên tìm thấy trong Database.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:**
    *   Triển khai cấu trúc Từ điển (Lookup Dictionary) trả về từ Database dưới định dạng `"{FullName} (ID: {PatientId})"`.
    *   Trong logic lưu trữ, nếu người dùng tự gõ tên mà không chọn từ danh sách gợi ý, hệ thống sẽ kiểm tra số lượng trùng tên trong DB. Nếu trùng từ 2 người trở lên, ứng dụng sẽ chặn lưu dữ liệu và yêu cầu người dùng click chọn chính xác tên kèm mã số ID từ menu gợi ý.

### 2.2. Treo ứng dụng do đồng bộ luồng bất đồng bộ (Deadlock on UI Thread)
*   **Vị trí:** `VisitFormDialog.xaml.cs` (phương thức lưu thông tin cũ).
*   **Mô tả:** Việc sử dụng các lệnh khóa luồng `.Result` hoặc `.Wait()` trong các hàm xử lý bất đồng bộ khi gọi database gây ra lỗi Deadlock đóng băng luồng giao diện chính.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:** Chuyển đổi toàn bộ các hàm hỗ trợ xác định thông tin ID bác sĩ và bệnh nhân sang bất đồng bộ (`async Task`) và sử dụng từ khóa `await` đầy đủ trong sự kiện click nút Lưu.

### 2.3. Lỗi sập ứng dụng khi truy vấn SQLite thất bại (Async Void Anti-Pattern)
*   **Vị trí:** Các sự kiện Click và TextChanged trong `VisitListView.xaml.cs` và `VisitFormDialog.xaml.cs`.
*   **Mô tả:** Khai báo hàm sự kiện dạng `async void` để thực thi truy vấn SQLite nhưng thiếu khối lệnh bắt lỗi (`try-catch`). Khi tệp database SQLite bị khóa hoặc gặp lỗi cấu trúc, ứng dụng crash và tự động tắt đột ngột mà không hiển thị thông tin.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:** Bao bọc toàn bộ code nghiệp vụ gọi dịch vụ Database trong khối `try-catch`. Khi xảy ra ngoại lệ (Exception), hiển thị thông báo lỗi trực quan lên màn hình bằng `ToastService.ShowError` hoặc `MessageDialog`.

### 2.4. Bất nhất giá trị trạng thái ca khám (Status Inconsistency)
*   **Vị trí:** Lớp thực thể [Visit.cs](file:///e:/ClinicMS/Models/Visit.cs) và logic tại View.
*   **Mô tả:** Tồn tại sự không đồng nhất giữa giá trị trạng thái lưu trong cơ sở dữ liệu `"In Progress"` (có dấu cách) và `"InProgress"` (không dấu cách), dẫn đến lỗi lọc nhóm ca khám hiển thị không chính xác.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:** Chuẩn hóa toàn bộ logic nghiệp vụ, hiển thị, gom nhóm và lưu trữ xuống Database về một định dạng chuỗi duy nhất: `"In Progress"`.

### 2.5. Xung đột theo dõi thực thể trong EF Core (Entity Tracking Conflict)
*   **Vị trí:** `DatabaseService.cs` (phương thức `AddVisitAsync` và `UpdateVisitAsync`).
*   **Mô tả:** Lỗi xảy ra khi thực hiện cập nhật đơn thuốc của ca khám cũ: `The instance of entity type 'Visit' cannot be tracked because another instance with the same key value is already being tracked`.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:**
    *   Tách biệt hoàn toàn danh sách chi tiết đơn thuốc (`PrescriptionDetails`) khỏi đối tượng `Visit` đầu vào khi lưu.
    *   Trong `UpdateVisitAsync`, thực hiện tìm bản ghi gốc từ DB thông qua ID, cập nhật giá trị thô (`SetValues`), xóa sạch chi tiết đơn thuốc cũ và nạp lại đơn thuốc mới để tránh xung đột theo dõi thực thể của EF Core.

---

## 3. PHÂN NHÓM LỖI HỆ THỐNG & ĐƯỜNG DẪN (ARCHITECTURAL & PATHS)

### 3.1. Fix cứng đường dẫn file Cơ sở dữ liệu SQLite
*   **Vị trí:** `ClinicDbContext.cs`.
*   **Mô tả:** Cấu hình đường dẫn kết nối SQLite cố định trỏ đến ổ đĩa `D:\` hoặc thư mục làm việc tuyệt đối của nhà phát triển, gây lỗi crash ngay khi ứng dụng khởi chạy trên máy khách.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:** Triển khai cơ chế phân biệt môi trường tự động:
    *   *Môi trường Debug:* Ưu tiên lấy file `clinic_database.db` trực tiếp trong thư mục nhị phân (bin/Debug) hoặc thư mục gốc của dự án.
    *   *Môi trường Publish:* Lưu trữ an toàn tại thư mục hệ thống `AppData\Local\ClinicMS` của người dùng hiện hành để tránh mất mát thông tin khi cài đặt lại app.

### 3.2. Quá tải RAM do tải tĩnh dữ liệu lớn (Static Memory Leak)
*   **Vị trí:** Các View hiển thị danh sách (`PatientListView`, `VisitListView`).
*   **Mô tả:** Ứng dụng load toàn bộ hàng ngàn bản ghi bệnh nhân cùng lúc vào DataGrid, dẫn đến việc tiêu tốn RAM cực lớn và làm giảm hiệu năng kết xuất của WPF.
*   **Trạng thái:** 🟢 **ĐÃ SỬA**.
*   **Cách khắc phục:**
    *   Triển khai phân trang cơ sở dữ liệu (**Paging**) với kích thước trang là 50 dòng thông qua `LIMIT/OFFSET`.
    *   Xây dựng cơ chế cuộn tải thêm (**Infinite Scrolling**) tự động khi người dùng cuộn ScrollBar xuống gần cuối danh sách.
    *   Áp dụng tính năng lọc tìm kiếm bất đồng bộ có độ trễ (**Debounce 300ms**) để tránh bắn liên tục các truy vấn thô vào file SQLite.

---

## 4. DANH SÁCH CÁC LỖI CHƯA SỬA TRONG MÃ NGUỒN (CURRENT UNFIXED BUGS & ISSUES)

Dưới đây là các lỗi và điểm yếu kỹ thuật hiện còn tồn tại trong mã nguồn ClinicMS cần được khắc phục để tránh nguy cơ sập ứng dụng hoặc sai lệch dữ liệu:

### 4.1. Lỗi Không Bắt Ngoại Lệ Trong Callback Bất Đồng Bộ (Unhandled Exceptions in Async Callbacks)
*   **Vị trí:** 
    *   `DoctorFormDialog.xaml.cs` (callback của `Modal.Show` trong `Save_Click`).
    *   `PatientFormDialog.xaml.cs` (callback của `Modal.Show` trong `Save_Click`).
    *   `MedicineFormDialog.xaml.cs` (callback của `Modal.Show` trong `Save_Click`).
    *   `ServiceFormDialog.xaml.cs` (callback của `Modal.Show` trong `Save_Click`).
*   **Mô tả:** Khi lưu dữ liệu thông qua hộp thoại xác nhận `ConfirmDialog`, logic gọi database được thực hiện bên trong hàm callback `async result => { ... }`. Do callback này chạy dưới dạng `async void` ngầm định, nếu có lỗi cơ sở dữ liệu xảy ra (ví dụ: DB bị khóa, lỗi kết nối hoặc vi phạm khóa ngoại), ngoại lệ sẽ không được bắt và sẽ trực tiếp làm sụp đổ ứng dụng (WPF Crash) ngay lập tức.
*   **Mức độ rủi ro:** 🔴 **CAO (Gây sập ứng dụng)**.
*   **Hướng khắc phục:** Bao bọc toàn bộ mã nguồn bên trong lambda callback của `Modal.Show` bằng khối `try-catch` và sử dụng `MessageDialog` để hiển thị lỗi an toàn cho người dùng thay vì để ứng dụng tự tắt.

### 4.2. Thiếu Ràng Buộc Kiểm Tra Số Âm Ở Ô Nhập Đơn Giá & Tồn Kho (Negative Input Vulnerability)
*   **Vị trí:** 
    *   `MedicineFormDialog.xaml.cs` (ô `PriceBox` và `StockBox`).
    *   `ServiceFormDialog.xaml.cs` (ô `PriceBox`).
*   **Mô tả:** Hệ thống hiện tại chỉ sử dụng `double.TryParse` hoặc `int.TryParse` để chuyển đổi kiểu dữ liệu. Điều này cho phép người dùng cố tình hoặc vô ý nhập đơn giá âm (ví dụ: `-10.000đ`) hoặc số lượng tồn kho âm (ví dụ: `-50` hộp thuốc). Hệ thống vẫn lưu trữ các giá trị âm này vào DB SQLite, dẫn đến lỗi tính tổng doanh thu hóa đơn bị âm và sai lệch báo cáo kiểm kho.
*   **Mức độ rủi ro:** 🟡 **TRUNG BÌNH (Sai lệch số liệu tài chính & kho)**.
*   **Hướng khắc phục:** Bổ sung điều kiện validation kiểm tra giá trị nhập vào phải `>= 0` trước khi cho phép xây dựng đối tượng lưu trữ.

---

## 5. HẠN CHẾ VỀ MẶT NGHIỆP VỤ & TÍNH NĂNG HỆ THỐNG (BUSINESS LOGIC & FUNCTIONAL GAPS)

Các điểm hạn chế dưới đây không hẳn là lỗi mã nguồn (code bug) mà là các khoảng trống nghiệp vụ cần được phát triển hoàn thiện ở các giai đoạn sau:

### 5.1. Độ Trễ Trong Việc Đồng Bộ Giảm Tồn Kho Thực Tế (Delayed Medicine Stock Deduction)
*   **Vị trí:** `InvoiceService.cs` (phương thức `AddPaymentAsync`).
*   **Mô tả:** Việc trừ số lượng tồn kho thực tế (`StockQuantity`) của thuốc chỉ diễn ra khi thủ quỹ thực hiện thêm giao dịch thanh toán thành công và hóa đơn chuyển sang trạng thái `"Paid"` hoặc `"Partial"`.
*   **Trạng thái:** 🟢 **ĐÃ KHẮC PHỤC**.
*   **Cách khắc phục:**
    *   Sử dụng cơ chế giữ chỗ (`ReservedQuantity`) trong bảng thuốc. Khi bác sĩ kê đơn hoặc cập nhật chi tiết đơn thuốc, hệ thống sẽ tự động tính lượng chênh lệch (delta) và cộng vào `ReservedQuantity` của thuốc tương ứng thông qua `SavePrescriptionDetailsAsync`.
    *   Ở màn hình khám bệnh (`VisitListView.xaml.cs`) và hộp thoại khám bệnh chuyên sâu (`VisitFormDialog.xaml.cs`), hệ thống thực hiện kiểm tra tồn kho bằng cách sử dụng thuộc tính `AvailableQuantity` (`StockQuantity - ReservedQuantity`). Điều này ngăn chặn việc bác sĩ khác kê khống thuốc vượt quá lượng tồn khả dụng thực tế.
    *   Lượng tồn kho thực tế (`StockQuantity`) chỉ thực sự bị trừ khi bệnh nhân thanh toán hóa đơn (`InvoiceService.cs`), đồng thời giải phóng lượng giữ chỗ tương ứng (`ReservedQuantity -= item.Quantity`).

### 5.2. Thiếu Màn Hình Nhập Kết Quả Dịch Vụ Cận Lâm Sàng (Missing Imaging/Lab Results UI)
*   **Vị trí:** Tab Dịch vụ cận lâm sàng.
*   **Mô tả:** Cơ sở dữ liệu SQLite và Model C# có bảng `service_orders` và trường `result` để lưu trữ kết quả chụp chiếu/xét nghiệm. Tuy nhiên, ứng dụng hiện tại hoàn toàn thiếu giao diện người dùng để kỹ thuật viên nhập kết luận kết quả dịch vụ cận lâm sàng.
*   **Hướng khắc phục:** Xây dựng thêm một phân hệ UI đơn giản cho phòng cận lâm sàng để tìm kiếm bệnh nhân và điền thông tin mô tả kết quả, đính kèm file/hình ảnh kết quả.

### 5.3. Lịch Hẹn Khám (Appointment) Chưa Liên Kết Chặt Chẽ Với Ca Khám (Visit)
*   **Vị trí:** Phân hệ Lịch hẹn.
*   **Mô tả:** Đã có bảng dữ liệu `appointments` trong SQLite, nhưng chưa có luồng nghiệp vụ tự động chuyển đổi từ một Lịch hẹn đã đặt sang trạng thái Ca khám chờ (`Waiting`) ở danh sách tiếp đón của Lễ tân khi bệnh nhân đến phòng khám.
*   **Hướng khắc phục:** Thêm nút "Tiếp nhận khám" trên danh sách Lịch hẹn để tự động sinh ra một `Visit` mới với trạng thái `"Waiting"`.

### 5.4. Thiếu Cơ Chế Sao Lưu Cơ Sở Dữ Liệu Tự Động (No Database Backup Mechanism)
*   **Vị trí:** Hệ thống.
*   **Mô tả:** Cơ sở dữ liệu SQLite được lưu tập trung trong một file duy nhất (`clinic_database.db`). Nếu file này bị hỏng (corrupted) do mất điện đột ngột hoặc lỗi hệ điều hành, phòng khám sẽ mất toàn bộ lịch sử bệnh án. Ứng dụng chưa có tính năng tự động sao lưu dữ liệu.
*   **Hướng khắc phục:** Bổ sung một luồng chạy nền tự động tạo bản sao lưu đóng gói của file `.db` sang một thư mục an toàn khác định kỳ hàng ngày.
