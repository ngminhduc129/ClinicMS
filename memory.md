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
*   **Vấn đề:** Thêm bệnh nhân thành công nhưng không hiện ra danh sách (do vòng lặp sự kiện vô tận và lỗi nạp dữ liệu).
*   **Giải pháp:** 
    - Sửa logic `ApplyFilter` trong `PatientListView.xaml.cs` để chặn vòng lặp nạp dữ liệu liên tục.
    - Thêm khối `try-catch` vào các sự kiện `Save_Click` để hiển thị thông báo lỗi cụ thể qua `MessageBox` nếu có lỗi hệ thống xảy ra.
    - Đảm bảo danh sách bệnh nhân luôn hiển thị người mới nhất lên đầu (sắp xếp `DESC` theo ID).
*   **Kết quả:** Trải nghiệm người dùng mượt mà, thông tin lỗi rõ ràng, dữ liệu cập nhật tức thì.

---

## 5. DANH SÁCH CÁC FILE QUAN TRỌNG ĐÃ CẬP NHẬT
1.  `Data\ClinicDbContext.cs`: Đồng bộ tên bảng `prescription_details`.
2.  `Services\DatabaseService.cs`: Nâng cấp logic lưu Visit kèm Transaction.
3.  `Views\VisitFormDialog.xaml.cs`: Sửa lỗi Deadlock và Validation Async.
4.  `Views\PatientListView.xaml.cs`: Sửa lỗi đồng bộ danh sách bệnh nhân.
5.  `BUGS.md`: Tài liệu rà soát các lỗi tồn đọng và hướng khắc phục.

---
*Ghi chú: Cấu trúc Database hiện tại đã rất "sạch" và chuẩn theo Model. Mọi thay đổi về Model sau này cần được cập nhật tương ứng vào Database.*
