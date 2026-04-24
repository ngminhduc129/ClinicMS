# Kế Hoạch Nâng Cấp Hệ Thống Tải Dữ Liệu (Data Loading Upgrade Plan)

Tài liệu này chi tiết hóa các bước nâng cấp hệ thống tải dữ liệu cho dự án **ClinicMS**, chuyển từ mô hình "Tải tĩnh" sang mô hình "Tải tối ưu" (Enterprise Data Strategy) để đảm bảo hiệu năng khi dữ liệu lớn.

---

## 1. Mục Tiêu (Goals)
- **Tốc độ:** Mở tab danh sách < 100ms.
- **Tiết kiệm:** Giảm 70-80% mức sử dụng RAM và CPU khi xử lý hàng nghìn bản ghi.
- **Trải nghiệm:** Cuộn danh sách mượt mà (60 FPS), không bị "đơ" khi tìm kiếm.
- **Chính xác:** Tìm kiếm trên toàn bộ cơ sở dữ liệu thay vì chỉ trên dữ liệu đã load.

---

## 2. Vấn Đề Hiện Tại (Current Issues)
1. **Load cứng 1000 bản ghi:** Tốn tài nguyên vô ích nếu người dùng chỉ xem 10-20 người đầu tiên.
2. **Lọc trong bộ nhớ (In-memory Filter):** Chỉ tìm thấy kết quả trong số 1000 người đã load. Bỏ sót dữ liệu nếu tổng số bệnh nhân > 1000.
3. **UI Rendering:** Nếu không cấu hình Virtualization đúng, WPF sẽ vẽ toàn bộ 1000 dòng gây lag khi cuộn.
4. **Dữ liệu thừa:** Load cả những thông tin nặng (Ghi chú dài, chẩn đoán...) vào danh sách tổng quan.

---

## 3. Giải Pháp Kỹ Thuật (Technical Solutions)

### A. Lớp Dữ Liệu (Database Service)
- **Phân trang (Paging):** Sử dụng `LIMIT 50 OFFSET X` cho mọi truy vấn danh sách.
- **Truy vấn tổng (Total Count):** Thêm hàm `SELECT COUNT(*)` cho từng bảng và từng kết quả tìm kiếm.
- **Tải "Nông" (Shallow Load):** Tạo các Query chỉ lấy các cột cần thiết cho List (ID, Name, Date, Status).

### B. Lớp Logic (ViewModel)
- **Infinite Scrolling:** Tự động gọi tải thêm 50 bản ghi tiếp theo khi người dùng cuộn đến cuối danh sách.
- **Server-side Search:** Gửi từ khóa xuống DB để tìm kiếm.
- **Debounce Search:** Đợi 300ms sau khi người dùng ngừng gõ mới thực hiện tìm kiếm để tránh quá tải DB.
- **Quản lý trạng thái:**
  - `IsLoading`: Hiển thị vòng quay loading.
  - `TotalCount`: Hiển thị tổng số bản ghi hiện có.
  - `HasMore`: Xác định còn dữ liệu để load tiếp hay không.

### C. Lớp Giao Diện (View - XAML)
- **UI Virtualization:** 
  ```xml
  VirtualizingStackPanel.IsVirtualizing="True"
  VirtualizingStackPanel.VirtualizationMode="Recycling"
  ```
- **Loading UI:** Thêm `ProgressBar` hoặc `Spinner` ở cuối danh sách khi đang tải thêm dữ liệu.
- **Skeleton Screen:** Hiển thị khung giả trong lúc chờ dữ liệu (nếu cần thiết).

---

## 4. Lộ Trình Thực Hiện (Roadmap)

### Bước 1: Chuẩn hóa DatabaseService
- Cập nhật các hàm `GetPatients`, `GetVisits` để hỗ trợ `limit` và `offset` chuẩn.
- Thêm hàm `GetSearchCount` để trả về tổng số kết quả tìm kiếm.

### Bước 2: Nâng cấp Tìm kiếm (Search)
- Chuyển `CollectionViewSource.Filter` sang gọi trực tiếp `SearchPatientsAsync` từ DB.
- Áp dụng `CancellationTokenSource` để hủy các yêu cầu tìm kiếm cũ khi người dùng gõ phím nhanh.

### Bước 3: Cấu hình Virtualization & Infinite Scroll
- Chỉnh sửa `VisitListView.xaml` và `PatientListView.xaml` để bật chế độ Recycling cho Virtualization.
- Thêm sự kiện bắt vị trí ScrollBar để kích hoạt lệnh `LoadMore`.

### Bước 4: Tải dữ liệu chi tiết (Lazy Load)
- Chỉ load `PrescriptionDetails` (Đơn thuốc) khi một `Visit` được chọn (SelectionChanged).

---

## 5. Chỉ Số Đánh Giá (KPIs)
- **RAM usage:** Duy trì ổn định dưới 200MB bất kể kích thước Database.
- **Search Latency:** Kết quả hiện ra trong < 500ms (bao gồm cả thời gian trễ gõ phím).
- **Data Integrity:** Con số "Tổng cộng" luôn khớp với số lượng thực tế trong DB.

---
*Người lập kế hoạch: Gemini CLI*
*Ngày: 31/03/2026*
