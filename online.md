# Kế hoạch chuyển ClinicMS từ WPF Desktop lên Web App

> Mục tiêu: Chuyển ứng dụng quản lý phòng khám ClinicMS từ WPF desktop thành web app,
> chạy trên 1 máy trong phòng khám, các máy khác truy cập bằng trình duyệt qua LAN.

---

## 1. Kiến trúc đích

```
┌───────────┐    LAN (WiFi)    ┌──────────────────────────────┐
│ PC1       │ ──► http://     │  Máy chủ (Windows 10/11)     │
│ (Browser) │                 │  ┌─────────────────────────┐  │
├───────────┤                 │  │ ASP.NET Core Web API    │  │
│ PC2       │                 │  │ (+Blazor Server UI)     │  │
│ (Browser) │                 │  ├─────────────────────────┤  │
├───────────┤                 │  │ SQLite (clinic_data.db)  │  │
│ Laptop    │                 │  │ trên ổ cứng máy chủ      │  │
│ (Browser) │                 │  └─────────────────────────┘  │
└───────────┘                 └──────────────────────────────┘
```

### Công nghệ

| Thành phần | Công nghệ |
|-----------|-----------|
| Backend | ASP.NET Core 8 Web API |
| Frontend | Blazor Server (.NET 8) |
| Database | SQLite (giữ nguyên) |
| ORM | Entity Framework Core 8 |
| PDF | QuestPDF (giữ nguyên) |
| Logging | Serilog (giữ nguyên) |

---

## 2. Cấu trúc Solution mới

```
E:\ClinicMS.Web\
│
├── ClinicMS.sln
│
├── src/
│   ├── ClinicMS.Shared/              # Class Library (net8.0)
│   │   ├── Models/                   # 15 entity classes (giữ nguyên, bỏ [NotMapped] UI)
│   │   │   ├── Patient.cs
│   │   │   ├── Doctor.cs
│   │   │   ├── Visit.cs
│   │   │   ├── Medicine.cs
│   │   │   ├── PrescriptionDetail.cs
│   │   │   ├── Prescription.cs
│   │   │   ├── ServiceOrder.cs
│   │   │   ├── Service.cs
│   │   │   ├── Invoice.cs
│   │   │   ├── InvoiceItem.cs
│   │   │   ├── Payment.cs
│   │   │   ├── Appointment.cs
│   │   │   ├── Contact.cs
│   │   │   ├── PatientContact.cs
│   │   │   └── AppSetting.cs
│   │   ├── Data/
│   │   │   └── ClinicDbContext.cs     # Sửa: đọc conn string từ config
│   │   └── ClinicMS.Shared.csproj
│   │
│   ├── ClinicMS.WebApi/               # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   │   ├── PatientsController.cs
│   │   │   ├── DoctorsController.cs
│   │   │   ├── VisitsController.cs
│   │   │   ├── MedicinesController.cs
│   │   │   ├── ServicesController.cs
│   │   │   ├── InvoicesController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── PdfController.cs
│   │   │   └── SettingsController.cs
│   │   ├── Services/                   # Copy từ WPF (giữ nguyên logic)
│   │   │   ├── CoreDatabaseService.cs  # Sửa nhẹ
│   │   │   ├── PatientService.cs
│   │   │   ├── DoctorService.cs
│   │   │   ├── VisitService.cs
│   │   │   ├── MedicineService.cs
│   │   │   ├── ServiceCatalogService.cs
│   │   │   ├── InvoiceService.cs
│   │   │   └── DashboardService.cs
│   │   ├── PdfExport/
│   │   │   └── PdfExportService.cs     # Sửa return byte[] thay vì file path
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── ClinicMS.WebApi.csproj
│   │
│   └── ClinicMS.Blazor/               # Blazor Server App
│       ├── Pages/
│       │   ├── Dashboard.razor
│       │   ├── Patients.razor
│       │   ├── PatientDetail.razor
│       │   ├── Doctors.razor
│       │   ├── Visits.razor
│       │   ├── VisitDetail.razor
│       │   ├── Medicines.razor
│       │   ├── Services.razor
│       │   ├── Invoices.razor
│       │   └── Settings.razor
│       ├── Components/
│       │   ├── Layout/
│       │   │   └── MainLayout.razor    # Sidebar + content
│       │   ├── NavMenu.razor
│       │   ├── ModalDialog.razor
│       │   ├── ToastNotification.razor
│       │   ├── StatCard.razor
│       │   ├── PatientForm.razor
│       │   ├── DoctorForm.razor
│       │   └── PaymentForm.razor
│       ├── wwwroot/
│       │   ├── css/
│       │   │   ├── app.css
│       │   │   ├── theme-light.css
│       │   │   └── theme-dark.css
│       │   └── js/
│       │       └── app.js              # localStorage, theme toggle
│       ├── Program.cs
│       └── ClinicMS.Blazor.csproj
│
└── tests/
    └── ClinicMS.Tests/                 # Sửa lại cho Web API
```

---

## 3. Kế hoạch thực hiện chi tiết

### Phase 1: Tạo Solution và Project Structure

**Thời gian:** 1 ngày

#### Bước 1.1 — Tạo solution
```bash
dotnet new sln -n ClinicMS -o E:\ClinicMS.Web
cd E:\ClinicMS.Web
mkdir src tests
```

#### Bước 1.2 — Tạo Shared Library
```bash
dotnet new classlib -n ClinicMS.Shared -o src\ClinicMS.Shared -f net8.0
dotnet sln add src\ClinicMS.Shared\ClinicMS.Shared.csproj
```
- Thêm NuGet: `Microsoft.EntityFrameworkCore.Sqlite` 8.0.0

#### Bước 1.3 — Tạo Web API
```bash
dotnet new webapi -n ClinicMS.WebApi -o src\ClinicMS.WebApi -f net8.0 --no-https
dotnet sln add src\ClinicMS.WebApi\ClinicMS.WebApi.csproj
```
- Thêm NuGet:
  - `QuestPDF` 2026.2.4
  - `Serilog` 4.3.1 + `Serilog.Sinks.File` 7.0.0
  - `Microsoft.Extensions.Caching.Memory` 8.0.1
- Thêm reference: `ClinicMS.Shared`

#### Bước 1.4 — Tạo Blazor Server
```bash
dotnet new blazorserver -n ClinicMS.Blazor -o src\ClinicMS.Blazor -f net8.0
dotnet sln add src\ClinicMS.Blazor\ClinicMS.Blazor.csproj
```
- Thêm reference: `ClinicMS.Shared`
- Thêm NuGet: `Microsoft.AspNetCore.Components.QuickGrid`

---

### Phase 2: Chuyển Models và Data Layer

**Thời gian:** 1 ngày

#### Bước 2.1 — Copy Models vào Shared

Copy 15 file từ `E:\ClinicMS\Models\*.cs` → `src\ClinicMS.Shared\Models\`

**Chỉnh sửa trên mỗi Model:**
- Xóa tất cả property `[NotMapped]` chỉ dùng cho UI (ví dụ: `DisplayId`, `StatusColor`, `Initials`, `FormattedTime`, ...)
- Giữ nguyên các data annotation: `[Required]`, `[Key]`, `[ForeignKey]`, `[Table]`, `[Column]`
- Giữ nguyên navigation properties

**Các UI helpers cần xóa:**

| Model | Properties xóa |
|-------|---------------|
| `Patient` | `Phone`, `Email`, `Address`, `Initials`, `DisplayId`, `DateOfBirthDisplay`, `GenderDisplay`, `HeightDisplay`, `WeightDisplay`, `NoteDisplay`, `PhoneDisplay`, `EmailDisplay`, `AddressDisplay` |
| `Doctor` | `DisplayId`, `Initials` |
| `Visit` | `PatientName`, `DoctorName`, `VisitTypeDisplay`, `StatusDisplay`, `StatusOrder`, `PatientInitials`, `PatientColor`, `PatientGenderIcon`, `GenderColor`, `GenderIconColor`, `StatusGroup`, `StatusDotColor`, `FormattedTime` |
| `Medicine` | `AvailableQuantity`, `StockPercent`, `StockStatus`, `StockStatusColor`, `StockStatusBg`, `StockStatusBorder`, `PriceDisplay`, `StockValue`, `StockValueDisplay` |
| `Service` | `PriceDisplay` |
| `Invoice` | `TotalDisplay`, `StatusDisplay`, `PatientName`, `DoctorName` |
| `InvoiceItem` | `Subtotal`, `PriceDisplay`, `SubtotalDisplay` |
| `Payment` | `AmountDisplay` |

#### Bước 2.2 — Sửa ClinicDbContext

**File:** `src\ClinicMS.Shared\Data\ClinicDbContext.cs`

Thay đổi:
- Bỏ static field `DbPath` và `OverrideDbPath`
- Nhận `DbContextOptions<ClinicDbContext>` qua constructor
- Không tự động xác định đường dẫn nữa

```csharp
public class ClinicDbContext : DbContext
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options)
        : base(options) { }

    // 15 DbSets giữ nguyên
    public DbSet<Patient> Patients { get; set; }
    // ... còn lại giữ nguyên

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Giữ nguyên toàn bộ mapping
    }
}
```

#### Bước 2.3 — Tạo appsettings.json

**File:** `src\ClinicMS.WebApi\appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ClinicData/clinic_database.db"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

#### Bước 2.4 — Cấu hình DbContext trong Program.cs

```csharp
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

### Phase 3: Chuyển Services (Business Logic)

**Thời gian:** 1 ngày

#### Bước 3.1-3.7 — Copy 7 Services

Copy từ `E:\ClinicMS\Services\*.cs` → `src\ClinicMS.WebApi\Services\`

**Chỉnh sửa mỗi Service:**

| Service | Thay đổi |
|---------|----------|
| `PatientService` | Constructor nhận `ClinicDbContext` thay vì `CoreDatabaseService` |
| `DoctorService` | Như trên |
| `VisitService` | Như trên. Giữ `DeleteVisitRelatedDataAsync` |
| `MedicineService` | Như trên |
| `ServiceCatalogService` | Như trên |
| `InvoiceService` | Như trên |
| `DashboardService` | Như trên |

**Pattern mới:**
```csharp
public class PatientService
{
    private readonly ClinicDbContext _context;

    public PatientService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<Patient>> GetPatientsWithContactsAsync(int offset, int limit)
    {
        var patients = await _context.Patients
            .Include(p => p.PatientContacts)
                .ThenInclude(pc => pc.Contact)
            .OrderBy(p => p.PatientId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
        return patients;
    }
    // ... giữ nguyên các method khác
}
```

**Giải thích:**
- Trong WPF, services dùng `CoreDatabaseService.Ctx()` để tạo context mới mỗi lần
- Trong ASP.NET Core, EF Core đã hỗ trợ sẵn unit of work pattern
- Mỗi request sẽ có 1 DbContext scope riêng

#### Bước 3.8 — CoreDatabaseService

Copy → `src\ClinicMS.WebApi\Services\CoreDatabaseService.cs`

Giữ lại các method `GetSettingAsync`, `SetSettingAsync`.
Bỏ method `Initialize()` — thay bằng migration khi khởi động.

Tạo `DbInitializer.cs` (Hosted Service) để tự động migrate DB khi app chạy:

```csharp
public class DbInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DbInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Seed dữ liệu mẫu (5 thuốc) nếu cần
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Đăng ký trong `Program.cs`:
```csharp
builder.Services.AddHostedService<DbInitializer>();
```

#### Bước 3.9 — PdfExportService

Copy → `src\ClinicMS.WebApi\PdfExport\PdfExportService.cs`

Thay đổi:
- Các method export trả về `byte[]` thay vì `string` (file path)
- Bỏ method `OpenFile()` (dùng `Process.Start` — WPF-specific)
- Thêm overload nhận trực tiếp dữ liệu từ controller

```csharp
public byte[] ExportPrescription(Visit visit)
{
    var document = new PrescriptionDocument(visit);
    return document.GeneratePdf(); // QuestPDF trả về byte[]
}
```

#### Bước 3.10 — Bỏ DatabaseService

File `DatabaseService.cs` chứa logic trùng lặp với các service khác.
**Không port file này.** Tất cả logic đã có trong các service chuyên biệt.

#### Bước 3.11 — Đăng ký DI trong Program.cs

```csharp
// Services
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<VisitService>();
builder.Services.AddScoped<MedicineService>();
builder.Services.AddScoped<ServiceCatalogService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<DashboardService>();

// Singleton
builder.Services.AddSingleton<CoreDatabaseService>();
builder.Services.AddSingleton<PdfExportService>();
```

---

### Phase 4: Tạo API Controllers

**Thời gian:** 1-2 ngày

#### Bước 4.1 — PatientsController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/patients` | `GetPatients(int offset, int limit, string? search)` | Danh sách + tìm kiếm + phân trang |
| `GET /api/patients/{id}` | `GetPatient(int id)` | Chi tiết 1 bệnh nhân |
| `GET /api/patients/{id}/visits` | `GetPatientVisits(int id)` | Lịch sử khám |
| `GET /api/patients/lookup` | `GetLookup(string? search)` | Cho autocomplete |
| `POST /api/patients` | `CreatePatient(PatientDto dto)` | Thêm mới |
| `PUT /api/patients/{id}` | `UpdatePatient(int id, PatientDto dto)` | Sửa |
| `DELETE /api/patients/{id}` | `DeletePatient(int id)` | Xóa |

#### Bước 4.2 — DoctorsController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/doctors` | `GetDoctors(string? search, int limit)` | Danh sách + tìm kiếm |
| `GET /api/doctors/{id}` | `GetDoctor(int id)` | Chi tiết |
| `GET /api/doctors/lookup` | `GetLookup()` | Cho autocomplete |
| `POST /api/doctors` | `CreateDoctor(DoctorDto dto)` | Thêm |
| `PUT /api/doctors/{id}` | `UpdateDoctor(int id, DoctorDto dto)` | Sửa |
| `DELETE /api/doctors/{id}` | `DeleteDoctor(int id)` | Xóa |

#### Bước 4.3 — VisitsController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/visits` | `GetVisits(int offset, int limit, string? dateFilter, string? search)` | Danh sách |
| `GET /api/visits/{id}` | `GetVisit(int id)` | Chi tiết (full include) |
| `GET /api/visits/{id}/prescriptions` | `GetPrescriptions(int id)` | Đơn thuốc |
| `GET /api/visits/{id}/services` | `GetServiceOrders(int id)` | Dịch vụ đã chỉ định |
| `GET /api/visits/today` | `GetTodayVisits()` | Danh sách khám hôm nay |
| `GET /api/visits/today/count` | `GetTodayCount()` | Số lượng hôm nay |
| `POST /api/visits` | `CreateVisit(VisitDto dto)` | Tạo phiếu khám |
| `PUT /api/visits/{id}` | `UpdateVisit(int id, VisitDto dto)` | Cập nhật (transaction) |
| `DELETE /api/visits/{id}` | `DeleteVisit(int id)` | Xóa |

#### Bước 4.4 — MedicinesController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/medicines` | `GetMedicines()` | Danh sách |
| `GET /api/medicines/{id}` | `GetMedicine(int id)` | Chi tiết |
| `POST /api/medicines` | `CreateMedicine(MedicineDto dto)` | Thêm |
| `PUT /api/medicines/{id}` | `UpdateMedicine(int id, MedicineDto dto)` | Sửa |
| `DELETE /api/medicines/{id}` | `DeleteMedicine(int id)` | Xóa |

#### Bước 4.5 — ServicesController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/services` | `GetServices()` | Danh sách |
| `POST /api/services` | `CreateService(ServiceDto dto)` | Thêm |
| `PUT /api/services/{id}` | `UpdateService(int id, ServiceDto dto)` | Sửa |
| `DELETE /api/services/{id}` | `DeleteService(int id)` | Xóa |

#### Bước 4.6 — InvoicesController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/invoices` | `GetInvoices(int offset, int limit, string? dateFilter, string? status)` | Danh sách + filter |
| `GET /api/invoices/{visitId}` | `GetInvoice(int visitId)` | Chi tiết hóa đơn |
| `GET /api/invoices/today/stats` | `GetTodayStats()` | Thống kê hôm nay |
| `POST /api/invoices/{visitId}/payments` | `AddPayment(int visitId, PaymentDto dto)` | Thanh toán (transaction) |

#### Bước 4.7 — DashboardController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/dashboard/stats` | `GetStats()` | 4 stat cards (patients, doctors, visits, today visits) |
| `GET /api/dashboard/chart` | `GetChart(int daysBack = 7)` | Dữ liệu biểu đồ cột |
| `GET /api/dashboard/today-visits` | `GetTodayVisits()` | Danh sách khám hôm nay |
| `GET /api/dashboard/appointments` | `GetAppointments(int limit = 5)` | Lịch hẹn sắp tới |

#### Bước 4.8 — PdfController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/pdf/prescription/{visitId}` | `ExportPrescription(int visitId)` | Trả về file PDF đơn thuốc |
| `GET /api/pdf/record/{visitId}` | `ExportMedicalRecord(int visitId)` | Trả về file PDF phiếu khám |
| `GET /api/pdf/invoice/{visitId}` | `ExportInvoice(int visitId, string? type)` | Trả về file PDF hóa đơn (full/medicine/service) |

#### Bước 4.9 — SettingsController

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/settings/{key}` | `GetSetting(string key)` | Lấy giá trị setting |
| `PUT /api/settings` | `UpdateSettings(Dictionary<string, string> settings)` | Cập nhật settings |

---

### Phase 5: Xây Frontend Blazor Server

**Thời gian:** 3-5 ngày

#### Bước 5.1 — MainLayout.razor (thay MainWindow.xaml)

Thành phần:
- Sidebar bên trái: Navigation buttons (Dashboard, Patients, Doctors, Visits, Medicines, Services, Invoices, Settings)
- Content area bên phải
- Toast notification container
- Modal dialog overlay

```razor
@inherits LayoutComponentBase

<div class="app-container @ThemeClass">
    <NavMenu />
    <main class="main-content">
        @Body
    </main>
    <ToastContainer />
    <ModalOverlay />
</div>
```

#### Bước 5.2 — NavMenu.razor (thay NavButton.xaml)

Các mục điều hướng:
- Dashboard (icon: biểu đồ)
- Patients (icon: người)
- Doctors (icon: bác sĩ)
- Visits (icon: khám bệnh)
- Medicines (icon: thuốc)
- Services (icon: dịch vụ)
- Invoices (icon: hóa đơn)
- Settings (icon: cài đặt)

#### Bước 5.3 — Dashboard.razor (thay DashboardView.xaml)

Layout:
- 4 StatCards: Tổng BN, Tổng BS, Tổng lượt khám, Khám hôm nay
- Biểu đồ cột 7 ngày (dùng Chart.js hoặc CSS thuần)
- Bảng "Khám hôm nay" (QuickGrid)
- Bảng "Lịch hẹn sắp tới" (QuickGrid)

#### Bước 5.4 — Patients.razor (thay PatientListView.xaml + PatientFormDialog.xaml)

Layout:
- Thanh tìm kiếm + nút "Thêm bệnh nhân"
- QuickGrid với phân trang (50/page)
- Cột: ID, Tên, Giới tính, Ngày sinh, SĐT, Hành động
- Modal form thêm/sửa bệnh nhân
- Click vào tên → mở PatientDetail

#### Bước 5.5 — PatientDetail.razor (thay PatientDetailDialog.xaml)

Layout:
- Thông tin bệnh nhân (tên, giới tính, ngày sinh, nhóm máu, chiều cao, cân nặng)
- Thông tin liên hệ (SĐT, Email, Địa chỉ)
- Lịch sử khám bệnh (QuickGrid + click xem chi tiết)

#### Bước 5.6 — Doctors.razor (thay DoctorListView.xaml + DoctorFormDialog.xaml)

Layout:
- QuickGrid: ID, Tên, Chuyên khoa, SĐT, Hành động
- Modal form thêm/sửa

#### Bước 5.7 — Visits.razor (thay VisitListView.xaml)

Layout:
- Bộ lọc ngày + ô tìm kiếm
- QuickGrid: ID, Bệnh nhân, Bác sĩ, Loại, Trạng thái, Ngày giờ, Hành động
- Trạng thái hiển thị bằng badge màu

#### Bước 5.8 — VisitDetail.razor (thay VisitFormDialog.xaml — phức tạp nhất)

Layout:
- **Header:** Bệnh nhân + Bác sĩ + Loại khám + Trạng thái
- **Vital signs:** Mạch, Huyết áp, Nhiệt độ, Cân nặng
- **Triệu chứng + Chẩn đoán + Ghi chú** (textarea)
- **Đơn thuốc:** Autocomplete tìm thuốc + số lượng + liều dùng + nút thêm
  - Bảng: Tên thuốc, SL, Liều dùng, Xóa
- **Dịch vụ:** Autocomplete tìm dịch vụ + nút thêm
  - Bảng: Tên dịch vụ, Giá, Xóa
- **Nút:** Lưu, Hoàn tất (chuyển Completed), Xuất PDF
- **Validation:** Tất cả field required

#### Bước 5.9 — Medicines.razor (thay MedicineListView.xaml + MedicineFormDialog.xaml)

Layout:
- Tabs: Tất cả / Còn hàng / Sắp hết / Hết hàng
- QuickGrid: Tên, Đơn vị, Giá, Tồn kho, Đã giữ, Còn lại, Badge trạng thái
- Modal form thêm/sửa

#### Bước 5.10 — Services.razor (thay ServiceListView.xaml + ServiceFormDialog.xaml)

Layout:
- QuickGrid: Tên dịch vụ, Giá, Hành động
- Modal form thêm/sửa
- Ô tìm kiếm (client-side filter)

#### Bước 5.11 — Invoices.razor (thay InvoiceListView.xaml + PaymentDialog.xaml)

Layout:
- Bộ lọc: Ngày + Trạng thái (Tất cả/Unpaid/Partial/Paid)
- QuickGrid: Mã (VisitId), Bệnh nhân, Bác sĩ, Tổng tiền, Trạng thái, Hành động
- Click "Thanh toán" mở PaymentDialog
- Nút xuất PDF (Full / Thuốc / Dịch vụ)

#### Bước 5.12 — Settings.razor (thay SettingsView.xaml)

Layout:
- Theme: Light / Dark (toggle)
- Font size: Nhỏ / Vừa / Lớn (radio)

---

### Phase 6: Component UI thay thế WPF-specific

**Thời gian:** 1-2 ngày

#### 6.1 — ToastNotification.razor

Thay `ToastService.cs` (WPF).

Cơ chế:
- Service quản lý danh sách toast (message, type, auto-dismiss sau 3s)
- Component render danh sách toast ở góc trên phải
- Dùng CSS animation: slide-in + fade-out

```csharp
// Services/ToastService.cs (bản Blazor)
public class ToastService
{
    public event Action<ToastItem>? OnShow;
    
    public void Show(string message, ToastType type = ToastType.Success)
    {
        OnShow?.Invoke(new ToastItem { Message = message, Type = type });
    }
    
    public void ShowError(string message) => Show(message, ToastType.Error);
}
```

#### 6.2 — ModalDialog.razor

Thay `ModalService.cs` (WPF).

Cơ chế:
- RenderFragment cho nội dung modal
- Overlay backdrop (CSS opacity)
- Stack hỗ trợ nested modal
- Callback `EventCallback<bool?>` khi đóng

```razor
@* ModalDialog.razor *@
@if (IsVisible)
{
    <div class="modal-overlay" @onclick="OnOverlayClick">
        <div class="modal-content" @onclick:stopPropagation>
            <div class="modal-header">
                <h3>@Title</h3>
                <button @onclick="Close">×</button>
            </div>
            <div class="modal-body">
                @Body
            </div>
            <div class="modal-footer">
                @Footer
            </div>
        </div>
    </div>
}
```

#### 6.3 — CSS Theming (thay ThemeManager.cs + ThemeColors.xaml)

File: `wwwroot/css/theme-light.css` và `wwwroot/css/theme-dark.css`

Sử dụng CSS custom properties:

```css
/* theme-light.css */
:root {
    --bg-primary: #ffffff;
    --bg-secondary: #f5f7fa;
    --bg-card: #ffffff;
    --text-primary: #1a1a2e;
    --text-secondary: #6b7280;
    --color-primary: #059669;
    --color-primary-hover: #047857;
    --border-color: #e5e7eb;
    --badge-success: #d1fae5;
    --badge-warning: #fef3c7;
    --badge-danger: #fee2e2;
    /* ... */
}

/* theme-dark.css */
[data-theme="dark"] {
    --bg-primary: #1a1a2e;
    --bg-secondary: #16213e;
    --bg-card: #0f3460;
    --text-primary: #e5e7eb;
    --text-secondary: #9ca3af;
    --color-primary: #10b981;
    /* ... */
}
```

Cơ chế toggle dùng JavaScript Interop (lưu vào localStorage):

```javascript
// wwwroot/js/app.js
window.applyTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
};
window.getTheme = () => localStorage.getItem('theme') || 'light';
```

#### 6.4 — CSS Font Scale (thay FontScaleManager.cs)

Sử dụng CSS custom property:

```css
:root {
    --font-scale: 1;
    --font-size-base: calc(14px * var(--font-scale));
    --font-size-sm: calc(12px * var(--font-scale));
    --font-size-lg: calc(16px * var(--font-scale));
    --font-size-xl: calc(20px * var(--font-scale));
    --font-size-2xl: calc(24px * var(--font-scale));
}

[data-font-scale="small"] { --font-scale: 0.85; }
[data-font-scale="medium"] { --font-scale: 1; }
[data-font-scale="large"] { --font-scale: 1.2; }
```

#### 6.5 — QuickGrid (thay DataGrid)

Dùng `QuickGrid` có sẵn trong .NET 8:

```razor
<QuickGrid Items="@patients" Pagination="@paginator">
    <PropertyColumn Property="@(p => p.PatientId)" Title="Mã" Sortable="true" />
    <PropertyColumn Property="@(p => p.FullName)" Title="Tên" Sortable="true" />
    <TemplateColumn Title="Giới tính">
        <span class="gender-badge @GetGenderClass(context.Gender)">
            @context.Gender
        </span>
    </TemplateColumn>
    <PropertyColumn Property="@(p => p.DateOfBirth)" Title="Ngày sinh" />
    <TemplateColumn Title="Hành động">
        <button @onclick="() => EditPatient(context)">Sửa</button>
        <button @onclick="() => DeletePatient(context)">Xóa</button>
    </TemplateColumn>
</QuickGrid>
```

#### 6.6 — Badge/Status (thay Converters.cs)

Dùng CSS class thay vì IValueConverter:

```css
.status-completed { background: #d1fae5; color: #065f46; }
.status-in-progress { background: #dbeafe; color: #1e40af; }
.status-waiting { background: #fef3c7; color: #92400e; }
.badge-in-stock { background: #d1fae5; color: #065f46; }
.badge-low-stock { background: #fef3c7; color: #92400e; }
.badge-out-of-stock { background: #fee2e2; color: #991b1b; }
```

#### 6.7 — Autocomplete (thay ComboBox tìm kiếm)

Component tìm kiếm với debounce:

```razor
<input @bind="searchTerm" @bind:event="oninput"
       placeholder="Tìm kiếm..."
       @oninput:preventDefault />
@if (results.Any())
{
    <ul class="autocomplete-dropdown">
        @foreach (var item in results)
        {
            <li @onclick="() => SelectItem(item)">@item.Name</li>
        }
    </ul>
}
```

---

### Phase 7: Authentication

**Thời gian:** 0.5 ngày

Vì app chạy trong LAN phòng khám, auth có thể tối giản:

#### Phương án A: Không auth (nội bộ)
- Bỏ qua authentication
- Chỉ cần tường lửa chặn port từ internet (mặc định)
- Thích hợp cho phòng khám nhỏ, tin cậy

#### Phương án B: Basic Auth đơn giản
- 1 tài khoản admin cố định
- Dùng ASP.NET Core Authentication với cookie

```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
```

- Thêm trang `Login.razor` với form username + password
- Password lưu trong `appsettings.json` hoặc biến môi trường

#### Phương án C: Windows Authentication
- Dùng `Microsoft.AspNetCore.Authentication.Negotiate`
- Tự động lấy tài khoản Windows đang đăng nhập
- Phù hợp nếu tất cả máy đều trong domain/workgroup

---

### Phase 8: Deploy lên máy chủ phòng khám

**Thời gian:** 0.5 ngày

#### Bước 8.1 — Chuẩn bị máy chủ

Yêu cầu tối thiểu:
- Windows 10/11 (bất kỳ)
- .NET 8 Runtime (hoặc publish self-contained)
- RAM ≥ 2GB (khuyên 4GB)
- Ổ cứng trống ≥ 100MB

#### Bước 8.2 — Publish ứng dụng

**Cách 1: Framework-dependent (cần cài .NET Runtime)**
```bash
cd src\ClinicMS.Blazor
dotnet publish -c Release -o ..\..\publish
```
→ Copy thư mục `publish` sang máy chủ
→ Cài .NET 8 Runtime trên máy chủ

**Cách 2: Self-contained (không cần cài .NET)**
```bash
cd src\ClinicMS.Blazor
dotnet publish -c Release -r win-x64 --self-contained true -o ..\..\publish
```
→ File lớn hơn (~60MB) nhưng chạy được ngay

#### Bước 8.3 — Chạy ứng dụng

Mở PowerShell (Run as Admin) trên máy chủ:

```powershell
# Mở port trên firewall
New-NetFirewallRule -DisplayName "ClinicMS" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow

# Chạy app
cd C:\path\to\publish
.\ClinicMS.Blazor.exe --urls "http://0.0.0.0:5000"
```

#### Bước 8.4 — Truy cập từ máy khác

Trên máy chủ: `http://localhost:5000`
Trên máy khác: `http://TEN_MAY_CHU:5000` hoặc `http://192.168.1.x:5000`

#### Bước 8.5 — Auto-start khi bật máy (tùy chọn)

Tạo shortcut trong `shell:startup`:
```powershell
$wshell = New-Object -ComObject WScript.Shell
$shortcut = $wshell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\ClinicMS.lnk")
$shortcut.TargetPath = "C:\ClinicMS\publish\ClinicMS.Blazor.exe"
$shortcut.Arguments = "--urls http://0.0.0.0:5000"
$shortcut.Save()
```

---

### Phase 9: Chạy thử và kiểm tra

**Thời gian:** 1 ngày

#### Test scenarios:

1. **Dashboard**
   - Stat cards hiển thị đúng số liệu
   - Biểu đồ 7 ngày có dữ liệu
   - Danh sách khám hôm nay + lịch hẹn

2. **Patient CRUD**
   - Thêm bệnh nhân mới (tự động tạo contact)
   - Tìm kiếm theo tên/SĐT
   - Sửa thông tin
   - Xóa (kiểm tra cascade)
   - Xem lịch sử khám

3. **Visit flow**
   - Tạo phiếu khám → Waiting
   - Cập nhật dấu hiệu, triệu chứng, chẩn đoán
   - Kê đơn thuốc (kiểm tra ReservedQuantity tăng)
   - Chỉ định dịch vụ
   - Hoàn tất → Completed → Invoice tự động tạo
   - Xuất PDF đơn thuốc + phiếu khám

4. **Invoice/Payment**
   - Xem hóa đơn sau khi visit completed
   - Thanh toán → StockQuantity giảm, ReservedQuantity xóa
   - Xuất PDF hóa đơn (3 loại)
   - Filter theo trạng thái/ngày

5. **Medicine inventory**
   - Thêm/sửa/xóa thuốc
   - Stock status badges đúng
   - Filter tabs hoạt động

6. **Appointments**
   - Tạo lịch hẹn
   - Hiển thị trên dashboard

7. **Settings**
   - Chuyển theme Light/Dark
   - Đổi font size

8. **Multi-user (cùng lúc 2-3 máy)**
   - 2 máy cùng xem danh sách
   - 1 máy tạo visit, máy kia xem invoice
   - Click cùng lúc — SQLite handle concurrent

---

## Tổng kết thời gian

| Phase | Công việc | Ngày |
|-------|-----------|------|
| 1 | Tạo solution + project structure | 1 |
| 2 | Models + Data Layer | 1 |
| 3 | Services (Business Logic) | 1 |
| 4 | API Controllers | 1-2 |
| 5 | Frontend Blazor (8-10 pages) | 3-5 |
| 6 | UI Components (Toast, Modal, Theme) | 1-2 |
| 7 | Authentication (tùy chọn) | 0.5 |
| 8 | Deploy | 0.5 |
| 9 | Kiểm tra | 1 |
| **Tổng** | | **~10-14 ngày** |

---

## Ghi chú quan trọng

### SQLite trên web
- SQLite hoạt động tốt với 1-5 concurrent users
- Không share file `.db` qua mạng — chỉ server app mới đọc/ghi
- File `.db` đặt trong `ClinicData/` cùng thư mục app, tự động backup

### Những thứ giữ được từ WPF
- 100% business logic trong Services
- 100% entity models (bỏ UI helpers)
- 100% PDF generation (QuestPDF)
- 100% database schema (EF Core + SQLite)

### Những thứ phải viết lại
- UI (XAML → Razor components)
- Data binding (WPF binding → Blazor binding)
- Commands (RelayCommand → Event handlers)
- Converters (IValueConverter → CSS classes)
- Toast/Modal services
- Theme/Font management
- PDF preview (WebView2 → file download)
