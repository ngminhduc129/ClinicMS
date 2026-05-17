# TỔNG HỢP TIÊU CHUẨN ANIMATION HỆ THỐNG (ANIMATION.MD)

Tài liệu này quy định các thông số kỹ thuật cho hiệu ứng chuyển động (Animation) trong ứng dụng ClinicMS nhằm đảm bảo trải nghiệm người dùng (UX) mượt mà và nhất quán.

---

## 1. TIÊU CHUẨN CHUNG
*   **Tốc độ (Duration):** 0.2 giây (200ms) - Cảm giác tức thì, dứt khoát.
*   **Đường cong (Easing):** `CubicEase` với `EasingMode.EaseOut` - Chuyển động chậm dần đều tự nhiên.
*   **Hiệu năng:** 
    *   Sử dụng `Timeline.SetDesiredFrameRate(..., 60)` để ép xung 60 FPS.
    *   Sử dụng lệnh `.Freeze()` cho các đối tượng animation để giảm tải CPU/GPU.

---

## 2. HIỆU ỨNG MỞ FORM (DIALOG OPENING)
Áp dụng cho: `PatientFormDialog`, `DoctorFormDialog`, `MedicineFormDialog`, `ServiceFormDialog`, `VisitFormDialog`.

### Thông số kỹ thuật:
| Thuộc tính | Giá trị | Mô tả |
| :--- | :--- | :--- |
| **Opacity** | 0.0 -> 1.0 | Làm mờ dần khi xuất hiện. |
| **TranslateY** | 15 -> 0 | Bay nhẹ từ dưới lên trên. |
| **Duration** | 0.2s | Thời gian hoàn tất hiệu ứng. |
| **RenderOrigin** | 0.5, 0.5 | Tâm điểm hiệu ứng tại giữa form. |

### Cấu trúc Code mẫu:
```csharp
var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
var dur = TimeSpan.FromSeconds(0.2);

var translate = new TranslateTransform(0, 15);
var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
var animTranslate = new DoubleAnimation(15, 0, dur) { EasingFunction = ease };

animOpacity.Freeze();
animTranslate.Freeze();

BeginAnimation(OpacityProperty, animOpacity);
translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
```

---

## 3. TỐI ƯU HÓA TRẢI NGHIỆM (ANTI-LAG)
Để tránh tình trạng giật (stutter) khi mở các form có dữ liệu lớn (như Bệnh án):
1.  **Pre-loading:** Nạp các danh sách tra cứu (Lookups) ngay trong Constructor của Form thay vì đợi `Loaded`.
2.  **Delay-loading:** Nếu form có logic xử lý nặng, thực hiện sau khi animation đã khởi chạy được ~50ms.
3.  **UI Virtualization:** Các bảng dữ liệu (DataGrid) luôn được bật chế độ `VirtualizingStackPanel.VirtualizationMode="Recycling"`.

---

## 4. QUY TRÌNH ÁP DỤNG
Mọi Form/Dialog mới khi được thêm vào hệ thống **BẮT BUỘC** phải tuân thủ chuẩn 0.2s/15px này để tránh gây cảm giác "chậm" hoặc "lạc lõng" so với các phần còn lại của ứng dụng.

---
*Cập nhật bởi Gemini CLI - 05/2026*
