# NHẬT KÝ CHỈNH SỬA HỆ THỐNG (MEMORY.MD)

Tài liệu này ghi lại các thay đổi quan trọng về cấu trúc Database, Logic xử lý và Hiệu năng đã được thực hiện để đảm bảo ứng dụng hoạt động ổn định và chính xác.

---

## 1. ĐỒNG BỘ HÓA & CHUẨN HÓA DATABASE (SQLITE)
*   **Vấn đề:** Tên cột trong Database không đồng nhất (viết hoa/thường lẫn lộn), thiếu Khóa chính (Primary Key) và thuộc tính Tự động tăng (Autoincrement) do các lỗi chuyển đổi trước đó.
*   **Giải pháp:** 
    - Thực hiện **Rebuild** toàn bộ Schema Database thông qua script Python (`rebuild_database_v2.py`).
    - Tất cả tên bảng và tên cột đã được chuyển về **lowercase** (chữ thường) để khớp 100% với thuộc tính `[Column("...")]` trong Model C#.
    - Khôi phục đầy đủ **Primary Key**, **Autoincrement** và **Foreign Key** (Khóa ngoại) cho tất cả các bảng.
*   **Kết quả:** Sửa triệt để lỗi `An error occurred while saving the entity changes`. Việc thêm mới bệnh nhân, bác sĩ, hồ sơ khám hiện tại đã hoạt động hoàn hảo.

---

## 2. SỬA LỖI TREO ỨNG DỤNG (DEADLOCK PREVENTION)
*   **Vấn đề:** Sử dụng `.Result` và `.Wait()` trong `VisitFormDialog.xaml.cs` gây khóa luồng chính (UI Thread).
*   **Giải pháp:** 
    - Chuyển các hàm `TryResolvePatientId` và `TryResolveDoctorId` thành phương thức bất đồng bộ `async Task`.
    - Sử dụng `await` trong sự kiện `Save_Click`.
*   **Kết quả:** Ứng dụng không còn bị treo ("đơ") khi nhấn nút Lưu hoặc khi tìm kiếm dữ liệu lớn.

---

## 3. HOÀN THIỆN LOGIC LƯU ĐƠN THUỐC (TRANSACTION-BASED)
*   **Vấn đề:** Đơn thuốc (Prescription Details) được thêm trong giao diện nhưng không được lưu xuống Database.
*   **Giải pháp:** 
    - Cập nhật `DatabaseService.cs` (hàm `AddVisitAsync` và `UpdateVisitAsync`).
    - Sử dụng **Database Transaction**: Đảm bảo cả Hồ sơ khám (Visit) và Đơn thuốc (Prescription) phải được lưu thành công cùng lúc. Nếu một trong hai lỗi, hệ thống sẽ tự động hoàn tác (Rollback) để tránh dữ liệu rác.
*   **Kết quả:** Đơn thuốc hiện tại đã được lưu trữ và truy xuất chính xác 100%.

---

## 4. TỐI ƯU HÓA GIAO DIỆN & PHẢN HỒI (UI/UX)
*   **Vấn đề:** 
    - Thêm bệnh nhân thành công nhưng không hiện ra danh sách (do vòng lặp sự kiện).
    - Thanh tìm kiếm bác sĩ bị lag khi gõ phím.
*   **Giải pháp:** 
    - Sửa logic `ApplyFilter` trong `PatientListView.xaml.cs` để chặn vòng lặp nạp dữ liệu.
    - Áp dụng kỹ thuật **Debounce (300ms)** và **CancellationTokenSource** vào `DoctorListView.xaml.cs` để tránh truy vấn database liên tục khi đang gõ.
    - Đồng bộ hiệu ứng **Smooth Scrolling (Pixel-based)** cho tất cả danh sách DataGrid.
*   **Kết quả:** Trải nghiệm người dùng mượt mà, tìm kiếm phản hồi tức thì.

---

## 5. SỬA LỖI TRUY VẤN ĐƠN THUỐC (EF CORE MAPPING)
*   **Vấn đề:** Lỗi `SQLite Error 1: no such column: p.PrescriptionVisitId` và `p.VisitId`.
*   **Giải pháp:** 
    - Thêm `[Column("...")]` vào Model `PrescriptionDetail` để khớp với database viết thường.
    - Loại bỏ sự dư thừa quan hệ trong class `Prescription`.
    - Cấu hình tường minh Fluent API trong `ClinicDbContext.cs` để EF Core không tự tạo cột "ảo" `PrescriptionVisitId`.
*   **Kết quả:** Chức năng đơn thuốc hoạt động ổn định, truy xuất đúng cột `visit_id` trong Database.

---

## 6. DANH SÁCH CÁC FILE QUAN TRỌNG ĐÃ CẬP NHẬT
1.  `Data\ClinicDbContext.cs`: Đồng bộ tên bảng `prescription_details`.
2.  `Services\DatabaseService.cs`: Nâng cấp logic lưu Visit kèm Transaction.
3.  `Views\VisitFormDialog.xaml.cs`: Sửa lỗi Deadlock và Validation Async.
4.  `Views\PatientListView.xaml.cs`: Sửa lỗi đồng bộ danh sách bệnh nhân.
5.  `BUGS.md`: Tài liệu rà soát các lỗi tồn đọng và hướng khắc phục.

## 7. CẢI THIỆN GIAO DIỆN TAB HỒ SƠ KHÁM (VISIT LIST UI)
*   **Vấn đề:** 
    - Các nút "In Bệnh Án" và "In Đơn Thuốc" hiển thị icon không phù hợp và màu sắc gây rối (quá nhiều nút màu xanh).
    - Người dùng phải cuộn xuống cuối trang mới nhấn được nút "Lưu".
    - Icon "In nhanh" trong danh sách làm rối giao diện.
*   **Giải pháp:** 
    - Loại bỏ các icon gây nhầm lẫn trong các nút In; chuyển nút "In Đơn Thuốc" sang dạng Outline để tập trung sự chú ý vào nút Lưu.
    - Thêm nút **Lưu (Save icon)** nhanh màu xanh lá cây ở vị trí ưu tiên nhất (góc trên cùng bên phải).
    - Hoán đổi vị trí nút Lưu và Chỉnh sửa để nút Lưu nằm ngoài cùng, dễ thao tác nhất.
    - Loại bỏ icon "In nhanh" khỏi danh sách bệnh nhân để giao diện tối giản và hiện đại.
*   **Kết quả:** Giao diện chuyên nghiệp, phân cấp thị giác rõ ràng (chỉ nút Lưu có màu xanh nổi bật), thao tác lưu dữ liệu cực kỳ nhanh chóng.

## 8. HOÀN THIỆN TAB DỊCH VỤ & NÂNG CẤP GIAO DIỆN (UI/UX)
*   **Triển khai Tab Dịch Vụ:**
    - Xây dựng đầy đủ tính năng CRUD (Thêm, Sửa, Xóa, Liệt kê) cho danh mục dịch vụ.
    - Đồng bộ giao diện theo tông màu Xanh lá (Green Theme) của hệ thống.
*   **Cải thiện hiển thị bảng dữ liệu:**
    - Áp dụng **Bo góc (CornerRadius: 10)** và **ClipToBounds** cho tất cả các bảng (DataGrid) tại tab Thuốc và Dịch vụ.
    - Khắc phục lỗi nội dung bảng bị tràn ra ngoài các góc bo, giúp giao diện trông hiện đại và tinh tế hơn.
*   **Kết quả:** Hệ thống đạt độ nhất quán cao về thẩm mỹ, các bảng dữ liệu hiển thị mượt mà với hiệu ứng bo góc chuyên nghiệp.

---
*Ghi chú: Cấu trúc Database hiện tại đã rất "sạch" và chuẩn theo Model. Mọi thay đổi về Model sau này cần được cập nhật tương ứng vào Database.*
