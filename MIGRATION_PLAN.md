# Kế Hoạch Chuyển SQLite → SQL Server — ClinicMS

## 1. Tổng Quan

Chuyển ứng dụng WPF ClinicMS từ **SQLite + EF Core** sang **SQL Server + EF Core**.

| Mục | Giá trị |
|-----|---------|
| Database hiện tại | SQLite file (`clinic_database.db`) |
| Database mới | SQL Server (Express/Full) |
| ORM | Entity Framework Core 8.0 |
| Migration strategy | EF Core Migrations |
| Connection string | `appsettings.json` |
| Tests | SQL Server LocalDB |

---

## 2. Danh Sách File Thay Đổi

| File | Trạng thái | Mô tả |
|------|-----------|-------|
| `ClinicMS.csproj` | Sửa | Thay Sqlite → SqlServer + Configuration + EF Tools |
| `ClinicMS.Tests\ClinicMS.Tests.csproj` | Sửa | Thêm SqlServer |
| `appsettings.json` | **Tạo mới** | Connection string + cấu hình |
| `Data\ClinicDbContext.cs` | Sửa | Xóa DbPath, dùng UseSqlServer + DI constructor |
| `Services\CoreDatabaseService.cs` | Sửa | Xóa raw SQL DDL, dùng Migrate() |
| `Services\DatabaseService.cs` | Sửa | Xóa raw SQL DDL, xóa TableExistsAsync() |
| `App.xaml.cs` | Sửa | Đơn giản hóa BackupDatabase() |
| `ClinicMS.Tests\ServiceTests.cs` | Sửa | Dùng LocalDB thay vì file SQLite |

---

## 3. Chi Tiết Từng Bước

### Bước 1: Cập nhật NuGet packages

**`ClinicMS.csproj`** — thay đổi:

```xml
<!-- XÓA dòng này: -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />

<!-- THÊM các dòng này: -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Thêm vào `PropertyGroup` để copy `appsettings.json` khi build:

```xml
<ItemGroup>
  <None Remove="appsettings.json" />
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**`ClinicMS.Tests\ClinicMS.Tests.csproj`** — thêm:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```

---

### Bước 2: Tạo `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ClinicMS;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

---

### Bước 3: Sửa `Data\ClinicDbContext.cs`

**Xóa toàn bộ:**
- Property `OverrideDbPath`
- Property `DbPath` (toàn bộ logic 30 dòng)

**Thêm:**
- Constructor nhận `DbContextOptions<ClinicDbContext>`

```csharp
public ClinicDbContext() { }

public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }
```

**Sửa `OnConfiguring`:**
- Xóa dòng `options.UseSqlite(...)`
- Không cần `OnConfiguring` nữa (DI đã config sẵn)

**Giữ nguyên:**
- `OnModelCreating` — tất cả mapping, composite keys, relationships (portable)
- Tất cả `DbSet<>` properties

---

### Bước 4: Thiết lập EF Core Migrations

```powershell
# Cài dotnet-ef tool (nếu chưa có)
dotnet tool install --global dotnet-ef

# Tạo migration đầu tiên
dotnet ef migrations add InitialCreate

# File migration sẽ được tạo trong Data\Migrations\
```

Migration sẽ tự động map toàn bộ model từ `OnModelCreating` thành SQL Server schema.

---

### Bước 5: Sửa `Services\CoreDatabaseService.cs`

**Xóa toàn bộ nội dung phương thức `Initialize()`:**
- Xóa `EnsureCreated()`
- Xóa tất cả `ExecuteSqlRaw` CREATE TABLE IF NOT EXISTS (10 câu)
- Xóa tất cả `ExecuteSqlRaw` ALTER TABLE (3 câu)
- Xóa seed medicines

**Thay thế bằng:**

```csharp
public static void Initialize()
{
    using var db = new ClinicDbContext();
    db.Database.Migrate();
    SeedMedicines(db);
}

private static void SeedMedicines(ClinicDbContext db)
{
    if (!db.Medicines.Any())
    {
        db.Medicines.AddRange(new List<Medicine>
        {
            new() { MedicineName = "Paracetamol 500mg", Unit = "Viên", Price = 2000, StockQuantity = 100, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Amoxicillin 500mg", Unit = "Viên", Price = 5000, StockQuantity = 50, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Siro Ho Prospan", Unit = "Chai", Price = 75000, StockQuantity = 20, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Vitamin C 1000mg", Unit = "Viên sủi", Price = 5000, StockQuantity = 30, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
            new() { MedicineName = "Hapacol 250mg", Unit = "Gói", Price = 3000, StockQuantity = 0, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
        });
        db.SaveChanges();
    }
}
```

---

### Bước 6: Sửa `Services\DatabaseService.cs`

**Xóa:**
- Tất cả `ExecuteSqlRaw` CREATE TABLE IF NOT EXISTS (7 câu, dòng 44-85)
- Phương thức `TableExistsAsync()` (dòng 820-840) — dùng `sqlite_master`

**Giữ nguyên:**
- Tất cả CRUD methods
- `DeleteVisitRelatedDataAsync()` (bỏ kiểm tra `TableExistsAsync`)
- Transactions

---

### Bước 7: Sửa `App.xaml.cs`

**Sửa `BackupDatabase()`:**
- Không copy file `.db` nữa
- Có thể thay bằng log backup hoặc bỏ qua (SQL Server tự quản lý backup)

```csharp
private void BackupDatabase()
{
    // SQL Server đã có cơ chế backup riêng
    // Bỏ qua hoặc log thông báo
    Log.Information("SQL Server tự quản lý backup, bỏ qua bước backup local.");
}
```

**Sửa `ConfigureServices()`:**
- Đăng ký `DbContext` với DI
- Đọc connection string từ `appsettings.json`

```csharp
private void ConfigureServices(IServiceCollection services)
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    services.AddDbContext<ClinicDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Các services giữ nguyên...
}
```

---

### Bước 8: Database initialization flow mới

Trong `App.OnStartup()`, thay đổi thứ tự:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // Logging
    Log.Logger = new LoggerConfiguration()...CreateLogger();

    // DI
    var services = new ServiceCollection();
    ConfigureServices(services);
    ServiceProvider = services.BuildServiceProvider();

    // Database
    using var scope = ServiceProvider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    db.Database.Migrate();
    DatabaseService.Initialize(); // seed data

    // ... culture, main window, exception handling
}
```

---

### Bước 9: Sửa Test Project

**`ServiceTests.cs`** — sửa `ServiceTestBase`:

```csharp
public class ServiceTestBase : IDisposable
{
    private readonly string _dbName = $"ClinicMS_Test_{Guid.NewGuid():N}";
    private readonly string _connectionString;

    protected ServiceTestBase()
    {
        _connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_dbName};Trusted_Connection=true;TrustServerCertificate=true;";
        ClinicDbContext.OverrideConnectionString = _connectionString;

        using var db = new ClinicDbContext();
        db.Database.EnsureCreated();
        // seed...
    }

    public void Dispose()
    {
        using var db = new ClinicDbContext();
        db.Database.EnsureDeleted();
    }
}
```

> **Lưu ý:** Cần sửa `ClinicDbContext` để hỗ trợ `OverrideConnectionString` tạm thời cho tests.

---

## 4. Model Mapping — SQLite → SQL Server

EF Core tự động map dựa trên kiểu C#:

| C# type | SQLite (cũ) | SQL Server (mới) |
|---------|-------------|------------------|
| `int` | `INTEGER` | `int` |
| `double` | `REAL` | `float` |
| `string` | `TEXT` | `nvarchar(max)` |
| `string?` | `TEXT` | `nvarchar(max)` |

Không cần `HasColumnType` — EF Core tự xử lý.

---

## 5. So Sánh Flow

### Flow cũ (SQLite)
```
OnStartup
  ├── BackupDatabase() → copy .db file
  ├── ConfigureServices → services.AddSingleton<DatabaseService>()
  ├── ClinicDbContext.OnConfiguring → UseSqlite(DbPath)
  ├── DatabaseService.Initialize()
  │    ├── EnsureCreated()
  │    └── CREATE TABLE IF NOT EXISTS (raw DDL)
  └── Show MainWindow
```

### Flow mới (SQL Server)
```
OnStartup
  ├── ConfigureServices
  │    ├── AddDbContext<ClinicDbContext>(UseSqlServer(connStr))
  │    └── services.AddSingleton<DatabaseService>()
  ├── DatabaseService.Initialize()
  │    ├── context.Database.Migrate()  // EF Core Migrations
  │    └── SeedMedicines()
  └── Show MainWindow
```

---

## 6. Lộ Trình Thực Hiện

| Thứ tự | Bước | File ảnh hưởng |
|--------|------|----------------|
| 1 | Cập nhật .csproj packages | 2 files |
| 2 | Tạo appsettings.json | 1 file mới |
| 3 | Sửa DbContext (DI + UseSqlServer) | `ClinicDbContext.cs` |
| 4 | Chạy `dotnet ef migrations add InitialCreate` | Tự sinh |
| 5 | Sửa CoreDatabaseService (xóa raw DDL) | `CoreDatabaseService.cs` |
| 6 | Sửa DatabaseService (xóa raw DDL) | `DatabaseService.cs` |
| 7 | Sửa App.xaml.cs (DI + backup) | `App.xaml.cs` |
| 8 | Sửa tests (LocalDB) | `ServiceTests.cs` |
| 9 | Build & test | — |
| 10 | Xóa file `.db` cũ (nếu có) | — |

---

## 7. Câu Hỏi Còn Lại

- **Tên database**: Mặc định `ClinicMS` — có muốn đổi không?
- **Authentication**: `Trusted_Connection=true` (Windows Auth) — có muốn dùng SQL Auth không?
- **String dates**: Hiện tại model dùng `string` cho ngày tháng. Giữ nguyên hay đổi sang `DateTime`?
