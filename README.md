# ClinicMS — WPF .NET 10 + SQLite

Ứng dụng quản lý phòng khám **chạy hoàn toàn offline** — không cần backend, không cần internet.
Dữ liệu lưu trực tiếp vào SQLite tại `%LocalAppData%\ClinicMS\clinic.db`.

---

## Kiến Trúc

```
React (fetch → API)           WPF (EF Core → SQLite)
─────────────────────         ──────────────────────────
ApiService (HttpClient)  →    DatabaseService (EF Core)
JSON REST API            →    ClinicDbContext (DbContext)
Remote server            →    clinic.db (local file)
```

## Cấu Trúc Dự Án

```
ClinicMS/
├── Data/
│   └── ClinicDbContext.cs      ← EF Core DbContext + seed data
├── Models/
│   ├── Patient.cs
│   ├── Doctor.cs
│   └── Visit.cs
├── Services/
│   ├── DatabaseService.cs      ← CRUD dùng EF Core (thay ApiService)
│   └── ToastService.cs
├── ViewModels/
│   ├── BaseViewModel.cs
│   └── MainViewModel.cs
├── Views/
│   ├── DashboardView           ← Thống kê tổng quan
│   ├── PatientListView / Form / Detail
│   ├── DoctorListView  / Form / Detail
│   ├── VisitListView   / Form
│   ├── NavButton, InfoRow, ConfirmDialog
│   └── ...
└── Converters/
```

## Build & Chạy

```bash
cd ClinicMS
dotnet run
```

Nếu `dotnet run` bị lỗi quyền ghi thư mục `.dotnet` hoặc first-time setup, dùng script:

```powershell
cd ClinicMS
powershell -ExecutionPolicy Bypass -File .\run.ps1
```

## Publish thành 1 file .exe duy nhất

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
# Output: bin\Release\net8.0-windows\win-x64\publish\ClinicMS.exe
```

> File .exe ~70MB (bao gồm .NET runtime + SQLite), chạy được trên máy chưa cài .NET.

## Dữ Liệu

- Lần đầu chạy: tự động tạo DB + thêm dữ liệu mẫu (4 bệnh nhân, 3 bác sĩ, 4 hồ sơ khám)
- File DB: `C:\Users\<tên>\AppData\Local\ClinicMS\clinic.db`
- Có thể mở bằng [DB Browser for SQLite](https://sqlitebrowser.org/) để xem trực tiếp
