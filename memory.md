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

## 9. NÂNG CẤP GIAO DIỆN ĐƠN THUỐC & HỆ THỐNG GỢI Ý (PRESCRIPTION UI & AUTOCOMPLETE)
*   **Vấn đề:** 
    - Giao diện kê đơn cũ đơn điệu, không chuyên nghiệp.
    - Ô nhập tên thuốc không có tính năng gợi ý (suggest) giống như khi tìm bệnh nhân, gây bất tiện khi nhập liệu.
    - Menu gợi ý thường xuyên bị nhảy ngược lên trên hoặc không lọc đúng nội dung đang gõ.
*   **Giải pháp:** 
    - **Đồng bộ giao diện hiện đại:** Thiết kế lại mục ĐƠN THUỐC với thanh điểm nhấn màu hồng, bảng tiêu đề xám nhạt và ô nhập liệu dạng thẻ trắng có bóng đổ (Shadow effect).
    - **Hệ thống gợi ý thông minh (Smart Suggest):** 
        - Triển khai logic lọc theo thời gian thực (Real-time filtering) cho danh sách thuốc.
        - Sử dụng `Dispatcher` và `ApplyTemplate` để đảm bảo menu gợi ý luôn mở đúng lúc, đúng vị trí.
        - Giới hạn số lượng gợi ý (10 dòng) và chiều cao menu (`180px`) để tránh tình trạng menu bị lật ngược (flip) lên trên ô nhập liệu.
    - **Tối ưu trải nghiệm gõ:** Giữ nguyên vị trí con trỏ chuột (Caret position) khi danh sách gợi ý đang thay đổi, giúp việc gõ tên thuốc mượt mà, không bị gián đoạn.
    - **Tự động nạp dữ liệu:** Hệ thống tự động tải danh sách thuốc từ Database ngay khi nhấn nút "+ Thêm thuốc", đảm bảo luôn có dữ liệu để gợi ý.
*   **Kết quả:** Việc kê đơn thuốc hiện tại cực kỳ nhanh chóng và thẩm mỹ, mang lại cảm giác chuyên nghiệp như các ứng dụng SaaS hiện đại.

## 10. KHẮC PHỤC LỖI XUNG ĐỘT TRACKING (EF CORE TRACKING CONFLICT)
*   **Vấn đề:** 
    - Lỗi `The instance of entity type 'Visit' cannot be tracked because another instance with the same key value is already being tracked`.
    - Lỗi này xảy ra khi cập nhật hồ sơ khám, do EF Core cố gắng theo dõi nhiều đối tượng có cùng ID trong cùng một phiên làm việc (DbContext context).
*   **Giải pháp:** 
    - **Tách biệt đối tượng (Entity Detachment):** Trong `VisitService.cs` và `DatabaseService.cs`, khi thực hiện `AddVisitAsync` và `UpdateVisitAsync`, hệ thống hiện tại sẽ tạo ra các bản ghi `PrescriptionDetail` mới hoàn toàn từ dữ liệu đầu vào thay vì đính kèm trực tiếp các đối tượng cũ.
    - **Xử lý thuộc tính điều hướng (Navigation Property):** Gán `v.PrescriptionDetails = new List<PrescriptionDetail>()` tạm thời trước khi lưu `Visit` để tránh việc EF Core tự động cố gắng tracking các thực thể liên quan gây xung đột.
    - **Đồng bộ thông báo UI:** Sửa logic trong `VisitListView.xaml.cs` để thông báo "Đã lưu thay đổi" chỉ xuất hiện sau khi hàm `UpdateAsync` trả về `true` (thành công thực sự).
*   **Kết quả:** Sửa triệt để lỗi crash/đỏ khi nhấn Lưu bệnh án. Dữ liệu được lưu an toàn và thông báo trên giao diện hoàn toàn chính xác.

## 11. BẮT NGOẠI LỆ TRONG CALLBACK BẤT ĐỒNG BỘ & CHẶN GIÁ TRỊ ÂM TRÊN FORM
*   **Vấn đề:** 
    *   Logic lưu trong callback `async void` của `Modal.Show` ở các form không bắt exception, dễ gây sập app khi lỗi DB SQLite.
    *   Người dùng có thể nhập đơn giá âm hoặc tồn kho âm gây sai lệch số liệu tài chính/kho.
*   **Giải pháp:**
    *   Bao bọc tất cả các database calls trong callback lưu của `DoctorFormDialog`, `PatientFormDialog`, `MedicineFormDialog`, `ServiceFormDialog` bằng khối lệnh `try-catch` an toàn.
    *   Thêm các câu lệnh validation `Price < 0` và `StockQuantity < 0` trên form thuốc và dịch vụ để ngăn chặn giá trị âm.
*   **Kết quả:** Ứng dụng hoạt động ổn định trước mọi lỗi tương tác DB và đảm bảo tính chính xác của số liệu tài chính.

## 12. TỰ ĐỘNG SAO LƯU CƠ SỞ DỮ LIỆU KHI KHỞI CHẠY (AUTO DATABASE BACKUP)
*   **Vấn đề:** SQLite lưu toàn bộ dữ liệu trong file duy nhất, có rủi ro mất mát dữ liệu cao nếu tệp tin bị hỏng (corrupted).
*   **Giải pháp:**
    *   Chuyển thuộc tính `DbPath` trong `ClinicDbContext.cs` thành `public` để truy cập toàn cục.
    *   Tích hợp hàm `BackupDatabase()` chạy trong sự kiện `OnStartup` của `App.xaml.cs`.
    *   Tự động copy file cơ sở dữ liệu dự phòng sang thư mục `%localappdata%\ClinicMS\Backups\`, chỉ lưu giữ tối đa 7 bản sao lưu gần nhất để tiết kiệm dung lượng.
*   **Kết quả:** Dữ liệu phòng khám luôn được bảo vệ an toàn với quy trình sao lưu tự động chạy nền.

---
## 13. FIX LỖI P0-P2 & NÂNG CẤP GIAO DIỆN CHI TIẾT HỒ SƠ
* **P0 (3 files):**
  - `Services/VisitService.cs:97-104` — Thêm `.Include(v => v.PrescriptionDetails).ThenInclude(pd => pd.Medicine)` và `.Include(v => v.ServiceOrders).ThenInclude(so => so.Service)` trong `GetVisitDetailsAsync` (PDF xuất thiếu thuốc & dịch vụ).
  - `Views/VisitListView.xaml.cs` — Fix memory leak (lambda CollectionChanged → named handler + Unloaded unsubscribe). Fix Save_Click reload loop (unsubscribe SelectionChanged trước UpdateAsync, try-finally).
  - `Views/PdfPreviewView.xaml.cs` — Constructor từ `MainViewModel` → `ModalService` (bỏ static DI lookup).
* **P1 (4 files):**
  - `Views/VisitFormDialog.xaml.cs` — Chuyển 4× `Refresh*List()` từ constructor → Loaded (control chưa sẵn sàng).
  - `Views/PatientFormDialog.xaml.cs` — Bọc `LoadDataAsync` trong try-catch + dialog báo lỗi.
  - `ViewModels/VisitViewModel.cs` — Inject `ModalService` qua DI thay vì `App.ServiceProvider.GetService`.
  - `Views/MedicineFormDialog.xaml.cs` — Check overflow `(int)s`: cap `int.MaxValue`, min 0.
* **P2 (4 files):**
  - `Views/ServiceFormDialog.xaml.cs` — Animation 0.4s→0.2s, bỏ scale, giữ translate-only.
  - `Data/ClinicDbContext.cs` — Xoá duplicate `ToTable("visits")`.
  - `Resources/Themes/ThemeScrollBar.xaml` (mới) — Scrollbar custom: rộng 8px, thumb `#CBD5E1`, radius 3px, hover tối hơn.
  - `Resources/Themes/ClinicTheme.xaml` — Merge ThemeScrollBar.xaml.
* **UI improvements:**
  - `ThemeColors.xaml` — BorderColorLight `#E5E7EB`→`#D1D5DB`, TextTertiaryColor `#9CA3AF`→`#6B7280`, FormLabel FontWeight→Medium.
  - `VisitListView.xaml` — Button Xoá style `DangerGhostBtn` (`#FEF2F2` bg, hover `#FEE2E2`, no border). Icon Lưu `&#xE74E;`(floppy)→`&#xE73E;`(checkmark).
* **History tab:**
  - Tách `LoadPatientHistoryAsync()`, gọi từ cả `ShowDetail` và `Save_Click` (history refresh sau save).
  - Bỏ filter `VisitId != v.VisitId` — completed visits include current.
  - `PatientService.GetPatientVisitsAsync` — Thêm `.Include(v => v.ServiceOrders).ThenInclude(so => so.Service)`.
  - `VisitListView.xaml` — Thêm section "DỊCH VỤ ĐÃ CHỈ ĐỊNH" trong timeline history.

## 14. FIX DROPDOWN/POPUP BỊ TRÔI TỌA ĐỘ & KHÔNG SCROLL ĐƯỢC
* **Vấn đề:** 
  - Dropdown của FormComboBox (chọn thuốc, dịch vụ, bệnh nhân, bác sĩ) trong `VisitFormDialog` hiển thị sai vị trí (trôi lệch khỏi ô input) sau khi scroll main content.
  - Khi fix bằng cách đóng dropdown khi scroll, dropdown không scroll được bằng con lăn chuột (mouse wheel) vì `e.Handled = true` chặn sự kiện đến ScrollViewer bên trong popup.
* **Giải pháp gốc (root cause):**
  - Popup của FormComboBox trong `VisitFormDialog.xaml` **thiếu `PlacementTarget="{Binding RelativeSource={RelativeSource TemplatedParent}}"`** — không có ràng buộc vị trí, dropdown lấy tọa độ sai khi scroll.
* **Phân biệt scroll trong popup vs ngoài main content:**
  - Dùng `PresentationSource.FromDependencyObject(e.OriginalSource)` so sánh với `PresentationSource.FromDependencyObject(this)`.
  - Popup với `AllowsTransparency=True` tạo cửa sổ riêng → `PresentationSource` khác.
  - **Scroll trong popup:** `PresentationSource` khác → `return` luôn, sự kiện chảy tự nhiên đến ScrollViewer bên trong dropdown.
  - **Scroll ngoài main content:** `PresentationSource` giống → đóng tất cả dropdown hiện tại → cuộn main ScrollViewer.
* **Kết quả:** Dropdown hiển thị đúng vị trí, scroll được bằng con lăn chuột, tự động đóng khi scroll main content.

## 15. NÂNG CẤP GIAO DIỆN TAB BỆNH NHÂN & CƠ CHẾ GỢI Ý (PATIENT AUTOCOMPLETE + SEARCH)
*   **Vấn đề:**
    - Các ComboBox chọn Bệnh nhân, Bác sĩ, Dịch vụ, Thuốc trong VisitFormDialog dùng `ComboBox` mặc định — không gõ tìm được, khó chọn khi danh sách dài.
    - Popup của các ComboBox bị lỗi "Ghost" (hiện ra sau modal overlay do là HWND riêng với `AllowsTransparency=True`).
    - ComboBox trong `VisitFormDialog` bị trôi tọa độ sau khi scroll main content.
    - Khi scroll, dropdown không scroll được bằng con lăn chuột.
*   **Giải pháp:**
    - **Chuyển ComboBox → TextBox + Popup:** Cả 4 ComboBox (Patient, Doctor, Service, Medicine) đều được thay bằng TextBox + Popup riêng chứa ListBox gợi ý. Lọc theo thời gian thực (real-time filtering) khi gõ.
    - **Guard chọn đúng:** `_isSelectingPatient` / `_isSuppressTextChanged` ngăn vòng lặp khi chọn từ Popup.
    - **Ghost popup fix:** Đóng tất cả Popup đồng bộ (`StaysOpen=false`) trước khi gọi `_vm.Modal.Show()` — xóa sạch lớp HWND popup trước khi modal overlay phủ lên.
    - **Scroll detachment fix:** `PreviewMouseWheel` handler so sánh `PresentationSource.FromDependencyObject` — nếu scroll từ popup (HWND khác) thì `return` để scroll bên trong popup; nếu scroll từ main content thì đóng popup + cuộn main ScrollViewer.
*   **Kết quả:** Tìm kiếm mượt mà như ComboBox edittable, không còn "bóng ma" popup sau overlay, scroll popup riêng biệt.

## 16. TỐI ƯU GIAO DIỆN THẺ THỐNG KÊ DASHBOARD (STAT CARDS)
*   **Vấn đề:** Màu sắc các icon thẻ thống kê chưa đồng nhất (mỗi card dùng màu khác nhau tùy tiện). CornerRadius quá nhỏ (8px), padding chưa đẹp.
*   **Giải pháp:**
    - **Palette icon cố định:** Card 1–2 (Teal) `IconBackground="#CCFBF1"` / `IconColor="#0F766E"`. Card 3 (Amber) `IconColor="#B45309"` (giữ bg `#FEF3C7`). Card 4 (Sky Blue) `IconBackground="#E0F2FE"` / `IconColor="#0369A1"`.
    - **Số liệu:** `#1F2937` (tất cả card).
    - **Label:** `#64748B`, `FontWeight="Medium"`.
    - **CornerRadius:** `12`, **Padding:** `20,24`.
    - **Chart bar:** Width `40`, dashed grid lines.
*   **Kết quả:** Giao diện dashboard đồng bộ, chuyên nghiệp.

## 17. FORM LỊCH HẸN (APPOINTMENTFORMDIALOG) & THÊM NHANH BỆNH NHÂN INLINE
*   **Vấn đề (mới):** Tab Lịch Hẹn cần form tạo appointment riêng với header riêng.
*   **Giải pháp:**
    - **AppointmentFormDialog (mới):** Modal form với Patient/Doctor ComboBox, DatePicker, TimeBox, Note. Header flat white + border trái xanh 4px + `Background="#F0F9F0"`. Subtitle `#6B7280`. Icon `#ECFDF5`/`#16A34A`.
    - **Nút "+" bên cạnh Patient:** 36×36, CornerRadius 8, hover hiệu ứng.
    - **Inline Quick-Add Container:** Khi click "+", thay vì mở popup modal, hiện container inline ẩn/hiện ngay bên dưới ComboBox. Gồm Name + Phone fields, nút "Lưu" xanh, nút "✕" đóng.
    - **InlineSave_Click:** Gọi `PatientService.AddPatientAsync`, thêm vào `PatientVM.Patients`, tự động chọn trong `PatientCombo`.
    - **Deleted QuickAddPatientDialog.xaml + .xaml.cs** — không cần modal riêng nữa.
*   **Kết quả:** Luồng thêm bệnh nhân nhanh không rời form, không popup chồng popup.

## 18. SỬA GHOST POPUP & OVERLAY (MODAL SERVICE + DIMMER Z-ORDER)
*   **Vấn đề:** Khi hiển thị `QuickAddPatientDialog` (và các modal con khác), dimmer (backdrop) phủ lên luôn parent modal — dimmer và overlay của child modal chồng chéo.
*   **Giải pháp:**
    - **ModalService dimmer fix:** `_host.Children.Insert(prevIndex, dimmer)` — chèn dimmer trước parent modal trong `Children` collection, dimmer chỉ làm mờ app background, không làm mờ parent modal.
    - **Child modal overlay removed:** QuickAddPatientDialog không còn overlay riêng (Grid có Background overlay) — chỉ dùng dimmer từ ModalService.
*   **Kết quả:** Overlay tầng tầng lớp lớp biến mất. Parent modal vẫn sáng, chỉ app background bị dim.

## 19. TINH CHỈNH GIAO DIỆN NHỎ (UI TWEAKS)
*   **Medicine filter buttons font:** Chuyển từ `Content="Tất cả"` → `<TextBlock FontSize="13" FontWeight="SemiBold" Foreground="#166534">` để đồng bộ font với Visit tab.
*   **MedicineListView UI alignment:** Xoá local `IconBtn`+`OutlineBtn` styles. Title icon 40→36. Table card Grid+shadow. DataGrid virtualized + `FilteredMedicines` binding. Badge đồng bộ Visit (`CornerRadius=10`, `BorderThickness=1`, `FontSize=12`, `FontWeight=SemiBold`, `Padding=8,4`).
*   **Medicine stock filter:** Dùng `AvailableQuantity` (không phải `StockQuantity`). Stats > 0 && <= 10. Active filter indicator (selected button green bg/border).
*   **Medicine.cs:** Thêm `StockStatusBorder` property (red/amber/emerald 200).
*   **MessageDialog error button:** Style trigger `Tag="Error"` → gray button (`#9CA3AF`, hover `#6B7280`).
*   **Cursor jumping fix:** Xoá `IsFocused` `BorderThickness` change (1.5→2) trong `ModernTextBoxStyle` — Border re-layout gây ScrollViewer re-measure → caret reset.
*   **Dropdown scroll fix:** Xoá `ScrollViewer` bao quanh 5 suggestion popups (VisitFormDialog, PatientFormDialog) — ScrollViewer > ListBox vô hiệu hoá ListBox internal scrolling.
*   **ComboBoxItem FontWeight:** `FontWeight="Normal"` trong VisitListView local style.
*   **NavButton active alpha:** 45→75 — active state dễ thấy hơn.
*   **Visit.cs GenderColor:** `Nữ` → `#F0FDF4`/`#059669`, `Nam` → `#F3F4F6`/`#6B7280`.
*   **DashboardView status badge:** Thêm `&#xE73E;` (checkmark Segoe MDL2) icon trước status text.
*   **Header icons:** VisitFormDialog/VisitListView header icon `&#xE8D6;`(music note) → `&#xE160;`(document).
*   **Border contrast:** Light `#D1D5DB`→`#9CA3AF`, dark `#374151`→`#4B5563`.

---
*Ghi chú: Cấu trúc Database hiện tại đã rất "sạch" và chuẩn theo Model. Mọi thay đổi về Model sau này cần được cập nhật tương ứng vào Database.*
