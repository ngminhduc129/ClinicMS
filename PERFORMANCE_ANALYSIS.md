# Báo cáo Phân tích Hiệu suất Khởi động - ClinicMS

Tài liệu này phân tích các nguyên nhân gây chậm trễ khi khởi động ứng dụng (Startup Latency) và đề xuất các giải pháp kỹ thuật để tối ưu hóa tốc độ phản hồi.

## 1. Các vấn đề chính (Root Causes)

### 1.1. Lỗi truy vấn "N+1" nghiêm trọng
Trong `MainViewModel.cs`, phương thức `LoadPatientsAsync` đang gặp lỗi hiệu suất kinh điển:
*   **Hiện trạng:** Ứng dụng tải danh sách bệnh nhân, sau đó lặp qua từng bệnh nhân để thực hiện một truy vấn riêng biệt lấy thông tin liên lạc (`GetPatientWithContactAsync`).
*   **Hậu quả:** Nếu có 100 bệnh nhân, ứng dụng thực hiện **101 lần truy vấn** vào SQLite. Mỗi lần truy vấn lại khởi tạo một `ClinicDbContext` mới, gây lãng phí tài nguyên và làm treo UI Thread trong thời gian dài.

### 1.2. Khởi tạo Database đồng bộ trên UI Thread
Trong `App.xaml.cs`, phương thức `OnStartup` thực hiện:
*   `DatabaseService.Initialize()`: Chạy hàng loạt lệnh `CREATE TABLE`, `ALTER TABLE` và `EnsureCreated()`.
*   Vì đây là lệnh đồng bộ (Synchronous), ứng dụng sẽ không hiển thị giao diện cho đến khi toàn bộ cấu trúc DB được kiểm tra và cập nhật xong.

### 1.3. Tải toàn bộ dữ liệu (Eager Loading) khi khởi động
Phương thức `LoadAllAsync` trong `MainViewModel` cố gắng tải:
*   Toàn bộ Bệnh nhân (kèm Contact).
*   Toàn bộ Bác sĩ.
*   Toàn bộ Lượt khám (`Visits`).
*   Toàn bộ Danh mục thuốc.
*   **Hậu quả:** Lượng dữ liệu đổ vào bộ nhớ quá lớn ngay lập tức khiến RAM tăng đột ngột và CPU xử lý chuyển đổi dữ liệu (Mapping) liên tục, làm chậm quá trình hiển thị Dashboard.

### 1.4. Quản lý DbContext chưa tối ưu
Mỗi phương thức trong `DatabaseService` đều sử dụng `await using var db = Ctx();`. Việc mở/đóng kết nối liên tục (đặc biệt là trong vòng lặp N+1) tạo ra overhead lớn cho hệ điều hành và SQLite.

---

## 2. Giải pháp đề xuất (Proposed Solutions)

### 2.1. Khắc phục lỗi N+1 bằng Eager Loading (EF Core)
Thay vì truy vấn từng người, hãy sửa lại `GetPatientsAsync` để lấy dữ liệu gộp trong một câu lệnh SQL duy nhất:
```csharp
// Sử dụng Join hoặc Include trong DatabaseService
public async Task<List<Patient>> GetPatientsWithContactsAsync() {
    await using var db = Ctx();
    // Logic: Join bảng Patients với PatientContacts và Contacts
}
```

### 2.2. Chuyển đổi sang Khởi tạo Bất đồng bộ
*   Hiển thị `MainWindow` ngay lập tức với trạng thái "Loading".
*   Chuyển `DatabaseService.Initialize()` sang một `Task.Run()` hoặc chạy bất đồng bộ để không chặn luồng giao diện.

### 2.3. Áp dụng Tải dữ liệu theo yêu cầu (Lazy Loading / Pagination)
*   **Dashboard:** Chỉ tải các thông số tổng hợp (Counts) và danh sách lượt khám trong ngày.
*   **Trang Bệnh nhân/Thuốc:** Chỉ tải dữ liệu khi người dùng click vào Tab tương ứng.
*   **Phân trang:** Sử dụng `.Skip(n).Take(m)` cho các danh sách dài để giới hạn số lượng record xử lý mỗi lần.

### 2.4. Tối ưu hóa Database Index
Bổ sung các Index (Chỉ mục) cho các cột thường xuyên dùng để tìm kiếm hoặc sắp xếp trong SQLite:
*   `PatientId` trong bảng `PatientContacts`.
*   `VisitTime` trong bảng `Visits`.
*   `FullName` trong bảng `Patients`.

---

## 3. Kết quả mong đợi
Sau khi áp dụng các thay đổi trên:
1.  **Tốc độ khởi động:** Giảm từ vài giây xuống dưới 1 giây.
2.  **Số lượng truy vấn:** Giảm từ hàng trăm xuống còn khoảng 3-5 truy vấn chính.
3.  **Trải nghiệm người dùng:** Giao diện xuất hiện tức thì, dữ liệu được tải mượt mà trong nền.
