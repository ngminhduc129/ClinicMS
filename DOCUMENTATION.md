# TÀI LIỆU HỆ THỐNG CLINICMS

> Phần mềm quản lý phòng khám đa khoa — WPF .NET 8.0 + EF Core + SQLite

---

## MỤC LỤC

1. [TỔNG QUAN](#1-tổng-quan)
2. [CẤU TRÚC PROJECT](#2-cấu-trúc-project)
3. [CÔNG NGHỆ SỬ DỤNG](#3-công-nghệ-sử-dụng)
4. [DATABASE](#4-database)
   - 4.1 Danh sách bảng
   - 4.2 Sơ đồ quan hệ
5. [DATA LAYER](#5-data-layer)
6. [MODELS (ENTITIES)](#6-models)
7. [SERVICES (BUSINESS LOGIC)](#7-services)
8. [VIEWMODELS](#8-viewmodels)
9. [VIEWS (GIAO DIỆN)](#9-views)
   - 9.1 Danh sách view
   - 9.2 Luồng điều hướng
10. [RESOURCES & THEMES](#10-resources--themes)
11. [CONVERTERS](#11-converters)
12. [DEPENDENCY INJECTION](#12-dependency-injection)
13. [TÍNH NĂNG CHÍNH](#13-tính-năng-chính)
14. [BUGS & HẠN CHẾ](#14-bugs--hạn-chế)
15. [LỊCH SỬ THAY ĐỔI](#15-lịch-sử-thay-đổi)

---

## 1. TỔNG QUAN

ClinicMS là ứng dụng desktop quản lý phòng khám đa khoa, cho phép:
- Quản lý bệnh nhân (thông tin, liên hệ, lịch sử khám)
- Quản lý bác sĩ (chuyên khoa, thông tin cá nhân)
- Quản lý hồ sơ khám bệnh (triệu chứng, chẩn đoán, đơn thuốc, chỉ định dịch vụ)
- Quản lý danh mục thuốc (tồn kho, giá, đặt trước)
- Quản lý danh mục dịch vụ
- Quản lý lịch hẹn
- Quản lý hóa đơn (thanh toán, xuất PDF)
- Xuất PDF: Đơn thuốc, Bệnh án, Hóa đơn
- Dashboard thống kê
- Dark mode / Light mode
- Tự động sao lưu database
- Scale font chữ

---

## 2. CẤU TRÚC PROJECT

```
ClinicMS/
├── App.xaml                       # Application resources (styles, converters)
├── App.xaml.cs                    # Entry point, DI setup, logging, backup
├── ClinicMS.csproj                # .NET 8.0 WPF project
├── Converters/
│   └── Converters.cs              # 15 WPF value converters
├── Data/
│   └── ClinicDbContext.cs         # EF Core DbContext (15 DbSets)
├── Models/                        # 15 entity classes
├── Resources/Themes/              # 7 XAML theme files
│   ├── ClinicTheme.xaml           # Master theme (merged dictionary)
│   ├── ThemeColors.xaml           # Light mode color palette
│   ├── ThemeColorsDark.xaml       # Dark mode color palette
│   ├── ThemeControls.xaml         # Buttons, TextBox, ComboBox, DataGrid styles
│   ├── ThemeScrollBar.xaml        # Custom scrollbar (8px, rounded)
│   ├── ThemeTypography.xaml       # Font sizes, weights, families
│   └── ThemeContainers.xaml       # Cards, Forms, navigation containers
├── Services/                      # 13 service files
│   ├── CoreDatabaseService.cs     # DB init, settings CRUD
│   ├── DashboardService.cs        # Dashboard queries
│   ├── DatabaseService.cs         # Legacy (patient/doctor lookups)
│   ├── DoctorService.cs           # Doctor CRUD
│   ├── FontScaleManager.cs        # Font scaling engine
│   ├── InvoiceService.cs          # Invoice generation + payment + stock deduction
│   ├── MedicineService.cs         # Medicine CRUD
│   ├── ModalService.cs            # Modal dialog stack manager
│   ├── PatientService.cs          # Patient CRUD + search
│   ├── ServiceCatalogService.cs   # Service CRUD
│   ├── ThemeManager.cs            # Light/Dark switcher
│   ├── ToastService.cs            # Toast notification system
│   ├── VisitService.cs            # Visit CRUD + prescriptions + service orders
│   └── PdfExport/
│       └── PdfExportService.cs    # QuestPDF document templates
├── ViewModels/                    # 11 ViewModel files
│   ├── BaseViewModel.cs           # INotifyPropertyChanged + validation
│   ├── ChartItem.cs               # Chart data model
│   ├── DashboardViewModel.cs      # Dashboard aggregation
│   ├── DoctorViewModel.cs         # Doctor list management
│   ├── InvoiceViewModel.cs        # Invoice list + payment
│   ├── MainViewModel.cs           # Root VM, navigation, settings
│   ├── MedicineViewModel.cs       # Medicine list management
│   ├── PatientDetailViewModel.cs  # Patient detail modal
│   ├── PatientViewModel.cs        # Patient list + search
│   ├── ServiceViewModel.cs        # Service list management
│   └── VisitViewModel.cs          # Visit list + prescription/service mgmt
└── Views/                         # 46 files (23 XAML + 23 code-behind)
    ├── MainWindow.xaml/.cs
    ├── DashboardView.xaml/.cs
    ├── PatientListView.xaml/.cs
    ├── PatientFormDialog.xaml/.cs
    ├── PatientDetailDialog.xaml/.cs
    ├── DoctorListView.xaml/.cs
    ├── DoctorFormDialog.xaml/.cs
    ├── DoctorDetailDialog.xaml/.cs
    ├── VisitListView.xaml/.cs
    ├── VisitFormDialog.xaml/.cs
    ├── MedicineListView.xaml/.cs
    ├── MedicineFormDialog.xaml/.cs
    ├── ServiceListView.xaml/.cs
    ├── ServiceFormDialog.xaml/.cs
    ├── InvoiceListView.xaml/.cs
    ├── AppointmentFormDialog.xaml/.cs
    ├── SettingsView.xaml/.cs
    ├── PdfPreviewView.xaml/.cs
    ├── PaymentDialog.xaml/.cs
    ├── MessageDialog.xaml/.cs
    ├── ConfirmDialog.xaml/.cs
    ├── NavButton.xaml/.cs
    └── InfoRow.xaml/.cs
```

---

## 3. CÔNG NGHỆ SỬ DỤNG

| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|----------|
| .NET | 8.0-windows | Runtime framework |
| WPF | Built-in | Desktop UI |
| EF Core | 8.0.0 | ORM - SQLite |
| SQLite | EF Core provider | Database |
| QuestPDF | 2026.2.4 | PDF generation |
| Serilog | 4.3.1 | Logging |
| DI | Microsoft.Ext.DI 10.0.7 | Dependency Injection |
| WebView2 | 1.0.3912.50 | PDF preview |

---

## 4. DATABASE

### 4.1 Danh sách bảng (15 tables)

| # | Bảng | Khóa chính | Mô tả |
|---|------|------------|-------|
| 1 | `appointments` | `appointment_id` (PK, AUTOINCREMENT) | Lịch hẹn |
| 2 | `AppSettings` | `Key` (PK, TEXT) | Cài đặt (key-value) |
| 3 | `contacts` | `contact_id` (PK, AUTOINCREMENT) | Thông tin liên hệ |
| 4 | `doctors` | `doctor_id` (PK, AUTOINCREMENT) | Bác sĩ |
| 5 | `invoices` | `visit_id` (PK) | Hóa đơn |
| 6 | `invoice_items` | `visit_id` + `item_type` + `item_id` (Composite PK) | Chi tiết hóa đơn |
| 7 | `medicines` | `medicine_id` (PK, AUTOINCREMENT) | Danh mục thuốc |
| 8 | `patients` | `patient_id` (PK, AUTOINCREMENT) | Bệnh nhân |
| 9 | `patient_contacts` | `patient_id` + `contact_id` (Composite PK) | Liên kết BN-LH |
| 10 | `payments` | `payment_id` (PK, AUTOINCREMENT) | Thanh toán |
| 11 | `prescriptions` | `visit_id` (PK) | Đơn thuốc (header) |
| 12 | `prescription_details` | `visit_id` + `medicine_id` (Composite PK) | Chi tiết đơn thuốc |
| 13 | `services` | `service_id` (PK, AUTOINCREMENT) | Danh mục dịch vụ |
| 14 | `service_orders` | `service_order_id` (PK, AUTOINCREMENT) | Chỉ định dịch vụ |
| 15 | `visits` | `visit_id` (PK, AUTOINCREMENT) | Hồ sơ khám |

### 4.2 Sơ đồ quan hệ

```
patients 1──* visits *──1 doctors
patients *──* contacts (qua patient_contacts)
visits 1──* prescription_details *──1 medicines
visits 1──* service_orders *──1 services
visits 1──1 invoices
invoices 1──* invoice_items
invoices 1──* payments
visits 1──1 prescriptions
```

---

## 5. DATA LAYER

### `Data/ClinicDbContext.cs`

- **Namespace:** `ClinicMS.Data`
- **Base class:** `DbContext`
- **15 DbSet properties** cho tất cả entities
- **`DbPath` (static):** Xác định đường dẫn SQLite database:
  1. Debug: File trong `BaseDirectory` hoặc thư mục gốc project
  2. Release: `%LOCALAPPDATA%\ClinicMS\clinic_database.db` (sao chép từ file ship nếu chưa có)
- **`OverridesDbPath` (static):** Cho phép ghi đè đường dẫn (dùng trong test)
- **`OnConfiguring`:** Cấu hình `UseSqlite(DbPath)`
- **`OnModelCreating`:** Cấu hình:
  - Table names (lowercase)
  - Composite primary keys:
    - `PatientContact`: `(PatientId, ContactId)`
    - `InvoiceItem`: `(VisitId, ItemType, ItemId)`
    - `PrescriptionDetail`: `(VisitId, MedicineId)`
  - Foreign key relationships
  - `AppSetting.ToTable("AppSettings")`

---

## 6. MODELS

### 6.1 `Patient` (`patients`)
| Field | Type | Ghi chú |
|-------|------|---------|
| `PatientId` | int (PK, AutoIncrement) | |
| `FullName` | string | |
| `DateOfBirth` | string? | `dd/MM/yyyy` display |
| `Gender` | string? | |
| `BloodType` | string? | |
| `HeightCm` | double? | |
| `WeightKg` | double? | |
| `Note` | string? | |
| `CreatedAt` | string? | |
| `Phone`, `Email`, `Address` | string? (NotMapped) | Loaded from contacts |
| `Initials` | string (NotMapped) | 2 ký tự đầu họ tên |

### 6.2 `Doctor` (`doctors`)
| Field | Type |
|-------|------|
| `DoctorId` | int (PK, AutoIncrement) |
| `FullName` | string |
| `Specialty` | string? |
| `Phone` | string? |
| `Email` | string? |
| `Note` | string? |
| `CreatedAt` | string? |

### 6.3 `Visit` (`visits`)
| Field | Type | Ghi chú |
|-------|------|---------|
| `VisitId` | int (PK, AutoIncrement) | |
| `PatientId` | int (FK → patients) | |
| `DoctorId` | int (FK → doctors) | |
| `AppointmentId` | int? (FK → appointments) | |
| `VisitTime` | string? | `yyyy-MM-dd HH:mm:ss` |
| `VisitType` | string | Walk-in/Appointment/Emergency/Follow-up |
| `Status` | string | Waiting/In Progress/Completed |
| `Symptoms` | string? | |
| `Diagnosis` | string? | |
| `Note` | string? | |
| `Pulse` | string? | |
| `BloodPressure` | string? | |
| `Temperature` | string? | |
| `Weight` | string? | |
| `PrescriptionText` | string? | (Legacy) |
| Navigation: `Patient`, `Doctor`, `PrescriptionDetails`, `ServiceOrders` | |

### 6.4 `Medicine` (`medicines`)
| Field | Type | Ghi chú |
|-------|------|---------|
| `MedicineId` | int (PK, AutoIncrement) | |
| `MedicineName` | string | |
| `Unit` | string? | Viên, Chai, Gói... |
| `Price` | double | |
| `StockQuantity` | int | Tồn kho thực tế |
| `ReservedQuantity` | int | Đã đặt trước (trong đơn chưa thanh toán) |
| `CreatedAt` | string? | |
| `AvailableQuantity` | int (NotMapped) | `StockQuantity - ReservedQuantity` |
| `StockStatus` | string (NotMapped) | "Hết hàng" / "Sắp hết" / "Còn hàng" |

### 6.5 `PrescriptionDetail` (`prescription_details`)
| Field | Type |
|-------|------|
| `VisitId` | int (FK → visits) |
| `MedicineId` | int (FK → medicines) |
| `Quantity` | double |
| `Dosage` | string? |
| Navigation: `Medicine`, `Visit` |

### 6.6 `ServiceOrder` (`service_orders`)
| Field | Type |
|-------|------|
| `ServiceOrderId` | int (PK, AutoIncrement) |
| `VisitId` | int (FK → visits) |
| `ServiceId` | int (FK → services) |
| `Result` | string? |
| Navigation: `Service` |

### 6.7 `Service` (`services`)
| Field | Type |
|-------|------|
| `ServiceId` | int (PK, AutoIncrement) |
| `ServiceName` | string |
| `Price` | double |

### 6.8 `Invoice` (`invoices`)
| Field | Type |
|-------|------|
| `VisitId` | int (PK, FK → visits) |
| `TotalAmount` | double |
| `Status` | string (Unpaid/Paid/Partial) |
| `IsDeducted` | int (0/1) |
| `CreatedAt` | string? |
| Navigation: `Visit`, `Items`, `Payments` |

### 6.9 `InvoiceItem` (`invoice_items`)
| Field | Type |
|-------|------|
| `VisitId` | int (FK → invoices) |
| `ItemType` | string (Medicine/Service) |
| `ItemId` | int |
| `ItemName` | string |
| `Quantity` | int |
| `UnitPrice` | double |
| `Subtotal` (NotMapped) | `Quantity * UnitPrice` |

### 6.10 `Payment` (`payments`)
| Field | Type |
|-------|------|
| `PaymentId` | int (PK, AutoIncrement) |
| `VisitId` | int (FK → invoices) |
| `Amount` | double |
| `PaymentMethod` | string? |
| `PaymentTime` | string? |

### 6.11 `Appointment` (`appointments`)
| Field | Type |
|-------|------|
| `AppointmentId` | int (PK, AutoIncrement) |
| `PatientId` | int (FK → patients) |
| `DoctorId` | int (FK → doctors) |
| `AppointmentTime` | string |
| `Status` | string (Scheduled/Cancelled/Completed) |
| `CreatedAt` | string? |

### 6.12 Other Models
- **`Contact`** (`contacts`): Phone, Email, Address
- **`PatientContact`** (`patient_contacts`): PatientId, ContactId, Relationship, IsPrimary
- **`Prescription`** (`prescriptions`): VisitId, CreatedAt (legacy header)
- **`AppSetting`**: Key, Value (không có table attribute, mapped trong DbContext)

---

## 7. SERVICES

### 7.1 `CoreDatabaseService` (Singleton)
- `Initialize()`: Tạo tables nếu chưa tồn tại, seed 5 mẫu thuốc, migration cột
- `Ctx()`: Tạo `ClinicDbContext` instance mới
- `GetSettingAsync(key)` / `SetSettingAsync(key, value)`: CRUD settings

### 7.2 `PatientService` (Transient)
- CRUD: `GetPatientsAsync`, `AddPatientAsync`, `UpdatePatientAsync`, `DeletePatientAsync`
- Search: `SearchPatientsAsync(name/phone)`, `GetPatientsSearchCountAsync`
- Lookups: `GetAllPatientNamesAsync`, `GetPatientsLookupAsync`, `GetPatientsWithContactsAsync`
- Detail: `GetPatientWithContactAsync`, `GetPatientByNameAsync`, `GetPatientVisitsAsync`
- Thêm bệnh nhân tự động tạo Contact + PatientContact

### 7.3 `DoctorService` (Transient)
- CRUD: `GetDoctorsAsync`, `AddDoctorAsync`, `UpdateDoctorAsync`, `DeleteDoctorAsync`
- Search: `SearchDoctorsAsync(name/specialty)`
- Xóa bác sĩ kèm xóa visits liên quan

### 7.4 `VisitService` (Transient)
- CRUD: `GetTodayVisitsAsync`, `GetVisitsAsync`, `SearchVisitsAsync`, `AddVisitAsync`, `UpdateVisitAsync`, `DeleteVisitAsync`
- Transaction-based: `AddVisitAsync` và `UpdateVisitAsync` dùng database transaction
- `GetVisitDetailsAsync`: Include Patient, Doctor, PrescriptionDetails.Medicine, ServiceOrders.Service
- `SavePrescriptionDetailsAsync`: Xóa cũ + thêm mới + cập nhật `ReservedQuantity`
- `SaveServiceOrdersAsync`: Xóa cũ + thêm mới

### 7.5 `InvoiceService` (Transient)
- `GetInvoicesAsync`, `GetInvoiceByVisitIdAsync`
- `GenerateOrUpdateInvoiceAsync(visitId)`: Chỉ sinh hóa đơn khi visit status = "Completed"
  - Gom thuốc và dịch vụ thành InvoiceItem
  - Xóa item cũ, thêm item mới
- `AddPaymentAsync`: Xử lý thanh toán, cập nhật trạng thái, trừ tồn kho thực tế (`IsDeducted`)

### 7.6 `MedicineService` (Transient)
- CRUD: `GetMedicinesAsync`, `AddMedicineAsync`, `UpdateMedicineAsync`, `DeleteMedicineAsync`

### 7.7 `ServiceCatalogService` (Transient)
- CRUD: `GetServicesAsync`, `AddServiceAsync`, `UpdateServiceAsync`, `DeleteServiceAsync`

### 7.8 `DashboardService` (Transient)
- `GetUpcomingAppointmentsAsync(limit)`: Lịch hẹn sắp tới
- `GetVisitCountByDayAsync(daysBack)`: Thống kê lượt khám theo ngày

### 7.9 `ModalService` (Singleton)
- Stack-based modal manager: `Show(content, callback, canClose)`, `Close(result?)`, `RequestClose()`
- Dimmer (backdrop) tự động: Khi modal con mở, chèn dimmer trước modal cha
- `IsHitTestVisible` management: Vô hiệu hóa tương tác modal cha khi có modal con

### 7.10 `ToastService` (Singleton)
- `Show(message, type)`: Thêm toast + tự động xóa sau 3s
- `ShowError(message)`: Toast lỗi (nền đỏ)

### 7.11 `PdfExportService` (Singleton)
- `ExportPrescription(visit)`: Đơn thuốc (A5)
- `ExportMedicalRecord(visit)`: Bệnh án (A4)
- `ExportInvoice(invoice)`: Hóa đơn đầy đủ
- `ExportMedicineInvoice(invoice)`: Hóa đơn thuốc
- `ExportServiceInvoice(invoice)`: Hóa đơn dịch vụ
- Sử dụng QuestPDF Fluent API để tạo document

### 7.12 `ThemeManager` (Static)
- `ApplyTheme(theme)`: Chuyển đổi giữa Light/Dark mode
- `CurrentTheme`: Lấy theme hiện tại

### 7.13 `FontScaleManager` (Static)
- `UpdateApplicationScale(scale)`: Scale toàn bộ font sizes (Small/Medium/Large)

### 7.14 `DatabaseService` (Singleton, Legacy)
- Chứa các phương thức patient/doctor lookups (tồn tại song song với các Service mới)

---

## 8. VIEWMODELS

### 8.1 `BaseViewModel`
- `INotifyPropertyChanged`, `INotifyDataErrorInfo`
- `SetProperty<T>(ref field, value)`: Set + notify
- `ValidateProperty(name)`: Validation bằng `ValidationAttribute`
- `RelayCommand`: ICommand implementation

### 8.2 `MainViewModel` (Singleton)
- Root ViewModel, inject tất cả sub-ViewModels
- `CurrentPage`: Điều hướng 8 pages (dashboard/patients/doctors/visits/medicines/services/invoices/settings)
- `FullPatientNames`, `FullDoctorNames`: Lookup dictionaries
- `FontSizeLevel`: Small/Medium/Large
- `SelectedTheme`: Light/Dark
- `NavigateCommand`: RelayCommand cho navigation
- Auto-load khi khởi tạo (`InitializeAndLoad`)

### 8.3 `DashboardViewModel`
- `TotalPatients`, `TotalDoctors`, `TotalVisits`, `VisitsToday`
- `TodayVisits` (ObservableCollection)
- `UpcomingAppointments` (ObservableCollection)
- `WeeklyStats` (ObservableCollection<ChartItem>)
- `LoadAsync()`: Load tất cả thống kê

### 8.4 `PatientViewModel`
- `Patients` (ObservableCollection), phân trang (50 items/page)
- `SearchAsync(term)`: Tìm kiếm theo tên/số điện thoại
- `HasMore`, `LoadMoreAsync()`: Load thêm
- CRUD: `AddAsync`, `UpdateAsync`, `DeleteAsync`
- `DataChanged` event: Thông báo MainViewModel

### 8.5 `DoctorViewModel`
- `Doctors` (ObservableCollection)
- `SearchAsync(term)`: Tìm theo tên/chuyên khoa
- CRUD + `DataChanged` event

### 8.6 `VisitViewModel`
- `Visits` (ObservableCollection), phân trang
- `SearchAsync(term)`, `SearchByDateAsync(date)`
- Prescription: `GetPrescriptionDetailsAsync`, `SavePrescriptionAsync`
- ServiceOrders: `GetServiceOrdersAsync`, `SaveServiceOrdersAsync`
- Export PDF: `ExportPrescriptionCommand`, `ExportMedicalRecordCommand`
- `GenerateOrUpdateInvoiceAsync` được gọi sau khi lưu prescription/service orders

### 8.7 `MedicineViewModel`
- `Medicines` (ObservableCollection)
- CRUD operations

### 8.8 `ServiceViewModel`
- `Services` (ObservableCollection)
- `SearchAsync(term)`: Filter client-side
- CRUD + `DataChanged` event

### 8.9 `InvoiceViewModel`
- `Invoices` (ObservableCollection)
- `StatusFilter`: All/Unpaid/Paid
- `PrintMedicineCommand`, `PrintServiceCommand`: Xuất PDF hóa đơn
- `AddPaymentAsync`: Ghi nhận thanh toán

### 8.10 `PatientDetailViewModel`
- Xem chi tiết bệnh nhân + lịch sử khám
- `Close()`, `Edit()`: Mở form sửa

---

## 9. VIEWS

### 9.1 Danh sách và chức năng

| View | Chức năng |
|------|-----------|
| **MainWindow** | Shell chính: Sidebar navigation + ContentArea + Toast + Modal overlay |
| **DashboardView** | 4 stat cards + biểu đồ cột 7 ngày + lịch hẹn + danh sách khám hôm nay |
| **PatientListView** | Danh sách bệnh nhân + search + filter + CRUD forms |
| **PatientFormDialog** | Form thêm/sửa bệnh nhân |
| **PatientDetailDialog** | Chi tiết bệnh nhân + lịch sử khám (timeline) |
| **DoctorListView** | Danh sách bác sĩ + search + CRUD |
| **DoctorFormDialog** | Form thêm/sửa bác sĩ |
| **DoctorDetailDialog** | Chi tiết bác sĩ |
| **VisitListView** | Danh sách hồ sơ khám + search + date filter + timeline history |
| **VisitFormDialog** | Form khám: Chọn BN/BS, vital signs, chẩn đoán, kê đơn thuốc (autocomplete), chỉ định dịch vụ |
| **MedicineListView** | Danh sách thuốc + stock status badges + filter tabs + CRUD |
| **MedicineFormDialog** | Form thêm/sửa thuốc |
| **ServiceListView** | Danh sách dịch vụ + CRUD |
| **ServiceFormDialog** | Form thêm/sửa dịch vụ |
| **InvoiceListView** | Danh sách hóa đơn + filter + payment + xuất PDF |
| **AppointmentFormDialog** | Form tạo lịch hẹn + inline quick-add patient |
| **SettingsView** | Cài đặt theme + font size |
| **PdfPreviewView** | Xem trước PDF (WebView2) |
| **PaymentDialog** | Form thanh toán |
| **MessageDialog** | Thông báo |
| **ConfirmDialog** | Xác nhận (Yes/No) |
| **NavButton** | Component nút điều hướng sidebar |
| **InfoRow** | Component hiển thị thông tin dạng hàng ngang |

### 9.2 Luồng điều hướng

```
MainWindow
├── Dashboard (mặc định)
├── Patients → PatientList → PatientFormDialog / PatientDetailDialog
├── Doctors  → DoctorList  → DoctorFormDialog / DoctorDetailDialog
├── Visits   → VisitList   → VisitFormDialog (kèm Prescription + Service)
├── Medicines → MedicineList → MedicineFormDialog
├── Services  → ServiceList  → ServiceFormDialog
├── Invoices  → InvoiceList  → PaymentDialog / PdfPreviewView
└── Settings
```

---

## 10. RESOURCES & THEMES

### Theme system (7 XAML files)

1. **`ThemeColors.xaml`** (Light): Color palette — green primary (#16A34A), backgrounds, borders, text, status colors
2. **`ThemeColorsDark.xaml`** (Dark): Dark mode palette — darker backgrounds, lighter text, adjusted status
3. **`ThemeControls.xaml`**: Control templates for:
   - Buttons: Primary (green), Secondary, Danger, Icon, Outline, FilterTab, GreenBtn, IconBtn
   - TextBox: `ModernTextBoxStyle` (rounded, green focus)
   - ComboBox: `ModernComboBoxStyle` + `ModernComboBoxItemStyle`
   - DataGrid: `ModernDataGridStyle` + headers + rows + cells (hover green, selected green)
   - All with animation triggers (MouseEnter/MouseLeave/Pressed)
4. **`ThemeScrollBar.xaml`**: Custom scrollbar (8px width, #CBD5E1 thumb, 3px radius)
5. **`ThemeTypography.xaml`**: Font sizes (PageTitle 30 → Helper 13), weights (Light → ExtraBold)
6. **`ThemeContainers.xaml`**: Card, FormSection, FormField, PageHeader, StatCard, Badge styles
7. **`ClinicTheme.xaml`**: Master merged dictionary — imports all above files

### Shadow effects
- `SoftShadow`: Inputs/small elements (6% opacity)
- `StandardShadow`: Cards/lists (6% opacity)
- `ElevatedShadow`: Dialogs/modals (16% opacity)
- `FocusShadow`: Green glow focus (20% opacity)
- `BrandButtonShadow`: Green glow button (30% opacity)

---

## 11. CONVERTERS

| # | Converter | Input → Output |
|---|-----------|----------------|
| 1 | `BoolToVisibility` | true → Visible, false → Collapsed |
| 2 | `InverseBoolToVisibility` | true → Collapsed, false → Visible |
| 3 | `NullToVisibility` | null/empty → Visible |
| 4 | `NotNullToVisibility` | null/empty → Collapsed |
| 5 | `PageToBold` (multi) | page == active → Bold |
| 6 | `StatusToBrush` | Waiting/In Progress/Completed → color |
| 7 | `StatusToBackground` | Status string → light background |
| 8 | `InvoiceStatusToBrush` | Unpaid/Partial/Paid → red/amber/emerald |
| 9 | `InvoiceStatusToBackground` | Unpaid/Partial/Paid → light bg |
| 10 | `InverseStatusToVis` | Paid → Collapsed |
| 11 | `ToastTypeToBrush` | Error → red bg, Success → green bg |
| 12 | `ToastTypeToForeground` | Error → red text, Success → green text |
| 13 | `ZeroToVisibility` | count == 0 → Visible |
| 14 | `IndexConverter` (multi) | Item + list → row number |
| 15 | `PercentageToHeightConverter` (multi) | % + maxHeight → chart column height |

---

## 12. DEPENDENCY INJECTION

Container: `Microsoft.Extensions.DependencyInjection` (configured in `App.xaml.cs:ConfigureServices`)

### Singleton (shared instances)
- `DatabaseService` — Legacy database access
- `CoreDatabaseService` — Core DB operations
- `ToastService` — Toast notifications
- `ModalService` — Modal dialog stack
- `PdfExportService` — PDF generation
- `MainViewModel` — Root ViewModel
- `MainWindow` — Main window

### Transient (new instance per injection)
- `MedicineService`, `DoctorService`, `PatientService`, `VisitService`
- `ServiceCatalogService`, `InvoiceService`, `DashboardService`
- `DashboardViewModel`, `PatientViewModel`, `DoctorViewModel`
- `VisitViewModel`, `MedicineViewModel`, `ServiceViewModel`, `InvoiceViewModel`

---

## 13. TÍNH NĂNG CHÍNH

### 13.1 Quản lý bệnh nhân
- Thêm/sửa/xóa bệnh nhân
- Tìm kiếm theo tên, số điện thoại
- Xem chi tiết + lịch sử khám (timeline)
- Phân trang (50 items/page)

### 13.2 Quản lý bác sĩ
- Thêm/sửa/xóa
- Tìm kiếm theo tên, chuyên khoa

### 13.3 Quản lý hồ sơ khám
- Tạo hồ sơ: Chọn bệnh nhân + bác sĩ (autocomplete)
- Vital signs: Mạch, huyết áp, nhiệt độ, cân nặng
- Chẩn đoán, triệu chứng, ghi chú
- Kê đơn thuốc (autocomplete medicine, dosage, quantity)
- Chỉ định dịch vụ (autocomplete service)
- Cập nhật trạng thái: Đang chờ → Đang khám → Hoàn thành
- Xuất PDF đơn thuốc + bệnh án
- Xem lịch sử khám của bệnh nhân

### 13.4 Quản lý thuốc
- Thêm/sửa/xóa thuốc
- Stock tracking: `StockQuantity`, `ReservedQuantity`, `AvailableQuantity`
- Trạng thái: Hết hàng/Sắp hết/Còn hàng (badge màu)
- Filter nhanh: Tất cả/Còn hàng/Sắp hết/Hết hàng

### 13.5 Quản lý dịch vụ
- Thêm/sửa/xóa dịch vụ
- Filter client-side

### 13.6 Quản lý hóa đơn
- Tự động sinh khi visit → Completed
- Gom thuốc + dịch vụ thành InvoiceItem
- Thanh toán: Ghi nhận payment + cập nhật trạng thái (Unpaid/Partial/Paid)
- Trừ tồn kho thực tế khi thanh toán
- Xuất PDF: Hóa đơn tổng/hóa đơn thuốc/hóa đơn dịch vụ

### 13.7 Lịch hẹn
- Form tạo lịch hẹn
- Inline quick-add patient (không mở modal riêng)

### 13.8 Dashboard
- 4 stat cards: Tổng BN, BS, hồ sơ, hôm nay
- Biểu đồ cột 7 ngày
- Danh sách khám hôm nay
- Lịch hẹn sắp tới

### 13.9 Settings
- Theme: Light/Dark
- Font size: Small/Medium/Large

### 13.10 Tự động sao lưu
- Backup database khi khởi động (`App.xaml.cs:BackupDatabase`)
- Lưu tối đa 7 bản backup gần nhất

### 13.11 Error handling
- Global exception handling (UI Thread, AppDomain, Task)
- Logging với Serilog (file logs)
- Toast notifications cho lỗi nghiệp vụ
- try-catch trong tất cả async callbacks

---

## 14. BUGS & HẠN CHẾ

> Xem file `BUG.md` và `BUGS.md` (nếu có) để biết danh sách đầy đủ.

Các hạn chế đã biết:
- `DatabaseService.cs` (legacy) tồn tại song song với các service mới — có thể gây nhầm lẫn
- Một số form dialog chưa được optimize cho dark mode

---

## 15. LỊCH SỬ THAY ĐỔI

> Xem file `memory.md` để biết chi tiết 19 mục thay đổi đã thực hiện.

Tóm tắt các mốc chính:
1. Đồng bộ database (lowercase, PK, FK, AutoIncrement)
2. Fix deadlock UI thread (async/await)
3. Transaction-based prescription saving
4. UI/UX optimization (debounce, smooth scroll)
5. EF Core mapping fix (PrescriptionDetail columns)
6. VisitList UI redesign
7. Service tab CRUD
8. Prescription autocomplete suggest
9. EF Core tracking conflict fix
10. Exception handling + negative value validation
11. Auto database backup
12. P0-P2 bug fixes
13. Dropdown ghost popup fix (PlacementTarget, PresentationSource)
14. Patient/Doctor/Service/Medicine autocomplete (TextBox + Popup)
15. Dashboard stat cards redesign
16. Appointment form + inline quick-add patient
17. Modal service dimmer Z-order fix
18. Multiple UI tweaks (scrollbar, fonts, colors, badges)

---

*Tài liệu được tạo tự động ngày 25/05/2026. Cập nhật khi có thay đổi.*
