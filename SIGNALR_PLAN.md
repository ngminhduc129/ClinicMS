# Kế hoạch thêm SignalR Real-time Sync

## Kiến trúc

```
Máy chủ (192.168.1.6)
├── SQL Server (port 1433)
└── SignalRServer.exe (port 5000) — ASP.NET Console App
        ▲
        │ WebSocket / Tailscale 100.102.85.116:5000
        ▼
Máy A ─── ClinicMS.exe (SignalR Client)
Máy B ─── ClinicMS.exe (SignalR Client)
Máy C ─── ClinicMS.exe (SignalR Client)
```

Luồng: **Máy A thêm BN → gửi tín hiệu → Hub → broadcast → Máy B, C tự động LoadAsync()**

---

## Danh sách thay đổi

| File | Trạng thái | Dòng |
|------|-----------|------|
| `SignalRServer/SignalRServer.csproj` | **Mới** | ~10 |
| `SignalRServer/Program.cs` | **Mới** | ~25 |
| `Services/SignalRClientService.cs` | **Mới** | ~100 |
| `appsettings.json` | Sửa | +1 |
| `ClinicMS.csproj` | Sửa | +1 (NuGet) |
| `App.xaml.cs` | Sửa | +5 |
| `MainViewModel.cs` | Sửa | +25 |
| `PatientViewModel.cs` | Sửa | +4 |
| `DoctorViewModel.cs` | Sửa | +3 |
| `MedicineViewModel.cs` | Sửa | +3 |
| `ServiceViewModel.cs` | Sửa | +3 |
| `VisitViewModel.cs` | Sửa | +3 |
| `InvoiceViewModel.cs` | Sửa | +2 |

---

## File mới

### 1. `SignalRServer/SignalRServer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### 2. `SignalRServer/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();
app.MapHub<DataSyncHub>("/hub");
app.Run();

public class DataSyncHub : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task NotifyDataChanged(string entityType)
    {
        await Clients.Others.SendAsync("OnDataChanged", entityType);
    }
}
```

### 3. `Services/SignalRClientService.cs`

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ClinicMS.Services;

public class SignalRClientService
{
    private HubConnection? _connection;
    private readonly string _hubUrl;
    private CancellationTokenSource? _reconnectCts;

    public event Action<string>? OnDataChanged;

    public bool IsConnected =>
        _connection?.State == HubConnectionState.Connected;

    public SignalRClientService(IConfiguration config)
    {
        _hubUrl = config["SignalRServer"]
            ?? throw new InvalidOperationException("SignalRServer URL not configured");
    }

    public async Task ConnectAsync()
    {
        try
        {
            _reconnectCts = new CancellationTokenSource();

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new ForeverRetryPolicy())
                .Build();

            _connection.On<string>("OnDataChanged", entityType =>
            {
                OnDataChanged?.Invoke(entityType);
            });

            _connection.Reconnecting += ex =>
            {
                Log.Warning(ex, "SignalR reconnecting...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += async id =>
            {
                Log.Information("SignalR reconnected: {Id}", id);
                await Task.CompletedTask;
            };

            _connection.Closed += async ex =>
            {
                Log.Warning(ex, "SignalR closed — reconnecting in 5s...");
                await Task.Delay(5000, _reconnectCts.Token);
                await ConnectAsync();
            };

            await _connection.StartAsync();
            Log.Information("SignalR connected to {Url}", _hubUrl);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SignalR connection failed — retry in 5s...");
            try
            {
                await Task.Delay(5000, _reconnectCts?.Token ?? CancellationToken.None);
                await ConnectAsync();
            }
            catch { /* app shutting down */ }
        }
    }

    public async Task NotifyDataChangedAsync(string entityType)
    {
        try
        {
            if (_connection?.State != HubConnectionState.Connected) return;
            await _connection.InvokeAsync("NotifyDataChanged", entityType);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SignalR notify failed (offline?)");
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _reconnectCts?.Cancel();
            if (_connection != null)
                await _connection.DisposeAsync();
        }
        catch { }
    }
}

public class ForeverRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var delay = Math.Min(5 * Math.Pow(2, retryContext.PreviousRetryCount), 30);
        return TimeSpan.FromSeconds(delay);
    }
}
```

---

## File cần sửa

### 4. `appsettings.json` — thêm dòng

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;..."
  },
  "SignalRServer": "http://100.102.85.116:5000/hub"
}
```

### 5. `ClinicMS.csproj` — thêm NuGet

```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
```

### 6. `App.xaml.cs` — đăng ký service + kết nối

Trong `ConfigureServices`:
```csharp
services.AddSingleton<SignalRClientService>();
```

Cuối `OnStartup` (sau `mainWindow.Show()`):
```csharp
var signalR = ServiceProvider.GetRequiredService<SignalRClientService>();
_ = signalR.ConnectAsync(); // fire-and-forget, không crash app
```

Trong `OnExit` (dùng timeout 2s, không treo app):
```csharp
protected override void OnExit(ExitEventArgs e)
{
    var signalR = ServiceProvider.GetRequiredService<SignalRClientService>();
    signalR.DisconnectAsync().Wait(TimeSpan.FromSeconds(2));
    Log.Information("Ứng dụng đang đóng. Tạm biệt!");
    Log.CloseAndFlush();
    base.OnExit(e);
}
```

### 7. `MainViewModel.cs`

- Inject `SignalRClientService signalR`
- Trong constructor (dùng `Dispatcher.InvokeAsync` vì callback từ thread khác):

```csharp
signalR.OnDataChanged += entityType =>
{
    System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
    {
        try
        {
            switch (entityType)
            {
                case "patients":   await PatientVM.LoadAsync(); break;
                case "doctors":    await DoctorVM.LoadAsync(); break;
                case "medicines":  await MedicineVM.LoadAsync(); break;
                case "services":   await ServiceVM.LoadAsync(); break;
                case "visits":     await VisitVM.LoadAsync(); break;
                case "invoices":   await InvoiceVM.LoadAsync(); break;
            }
            await LoadLookupsAsync();
            await DashboardVM.LoadAsync();
        }
        catch { }
    });
};
```

### 8. Các ViewModel (Patient, Doctor, Medicine, Service, Visit, Invoice)

Mỗi ViewModel:
- Inject `SignalRClientService _signalR` qua constructor
- Trong `AddAsync`, `UpdateAsync`, `DeleteAsync` — thêm sau khi thành công:

```csharp
await _signalR.NotifyDataChangedAsync("ten_entity");
```

---

## Cách chạy

### Máy chủ:
```powershell
cd SignalRServer
dotnet publish -c Release -o publish
# Chạy lần đầu (cần cho phép firewall):
SignalRServer\publish\SignalRServer.exe
```

### Build ClinicMS:
```powershell
dotnet publish -c Release
# Copy thư mục publish sang các máy khác
```

### Ghi chú:
- Cần mở port 5000 trên Windows Firewall (lần đầu chạy sẽ tự hỏi)
- Khi máy chủ tắt → app vẫn chạy, chỉ mất real-time sync (không crash)
- Các nút Refresh vẫn hoạt động bình thường
- SignalR callback dispatch qua UI thread để tránh lỗi threading
