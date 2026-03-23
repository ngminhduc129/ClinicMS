# Kế Hoạch Nâng Cấp Tính Năng Hồ Sơ Khám (Prescription & Visit Details)

Tài liệu này trình bày kế hoạch chi tiết để hoàn thiện tính năng Hồ sơ khám, tập trung vào việc quản lý đơn thuốc (Prescription) và đồng bộ dữ liệu, trong khi vẫn giữ nguyên phong cách UI hiện tại.

## 1. Mục Tiêu
- Hoàn thiện tính năng kê đơn thuốc trong `VisitFormDialog`.
- Hiển thị danh sách thuốc đã kê trong `VisitListView` (Chi tiết hồ sơ).
- Đảm bảo lưu trữ và truy xuất dữ liệu đơn thuốc chính xác từ cơ sở dữ liệu SQLite.
- Giữ nguyên thiết kế UI (màu sắc, layout, animation).

## 2. Các Thành Phần Thay Đổi

### A. Model (Dữ liệu)
- **Visit.cs**: Bổ sung `public ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();` để quản lý quan hệ 1-N với chi tiết đơn thuốc.
- **PrescriptionDetail.cs**: Đảm bảo các thuộc tính `Quantity` và `Dosage` được ánh xạ đúng.

### B. Database Service & ViewModel
- **DatabaseService.cs**:
    - Thêm phương thức `GetPrescriptionDetailsAsync(int visitId)` để lấy danh sách thuốc theo hồ sơ.
    - Cập nhật `AddVisitAsync` và `UpdateVisitAsync` để lưu cả thông tin thuốc (Transaction-based).
- **MainViewModel.cs**:
    - Bổ sung logic xử lý lưu đơn thuốc.
    - Cập nhật danh sách `Visits` khi có thay đổi về đơn thuốc.

### C. UI Logic (Code-behind)
- **VisitFormDialog.xaml.cs**:
    - Hoàn thiện phương thức `Save_Click` để thu thập dữ liệu từ `_prescriptionItems` và gửi xuống ViewModel.
    - Cập nhật logic `LoadData` để hiển thị các liều dùng thuốc cũ khi chỉnh sửa hồ sơ.
- **VisitListView.xaml.cs**:
    - Thay thế placeholder `AddMedicineDetail_Click` bằng logic mở form chỉnh sửa đơn thuốc hoặc thêm thuốc nhanh.
    - Hoàn thiện hàm `ShowDetail` để render danh sách thuốc vào `MedicineList` (StackPanel) bằng code-behind (tương tự cách render `VisitGroupList`).

## 3. Lộ Trình Thực Hiện (Tasks)

| STT | Nhiệm vụ | Chi tiết |
|:---:|:---|:---|
| 1 | Cập nhật Model | Thêm collection `PrescriptionDetails` vào class `Visit`. |
| 2 | Cập nhật DB Service | Viết hàm lưu/xóa `PrescriptionDetail` theo `VisitId`. |
| 3 | Logic Form | Kết nối `_prescriptionItems` vào quy trình lưu Visit. |
| 4 | Hiển thị chi tiết | Viết hàm `RenderPrescription(Visit v)` để vẽ UI danh sách thuốc trong màn hình chi tiết. |
| 5 | Kiểm thử | Kiểm tra lưu mới, sửa hồ sơ cũ, xóa thuốc trong đơn. |

## 4. Ghi Chú về UI
- Tiếp tục sử dụng `DetailCardStyle` và các Resource màu sắc (`C.Gray400`, `C.Gray600`, ...) đã định nghĩa.
- Sử dụng các Icon và Border bo góc (10-16px) để đảm bảo tính nhất quán.
- Animation `AnimateOpen` trong Dialog được giữ nguyên để duy trì trải nghiệm người dùng.

---
*Người lập kế hoạch: Gemini CLI*
*Ngày: 22/03/2026*
