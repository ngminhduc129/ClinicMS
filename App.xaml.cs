using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ClinicMS.Services;
using ClinicMS.Views;
using ClinicMS.ViewModels;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ClinicMS.Data;

namespace ClinicMS;

    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        private static Process? _signalRServerProcess;

        private void StartSignalRServer()
        {
            try
            {
                var config = ServiceProvider?.GetService<IConfiguration>();
                var runLocalStr = config?["RunLocalSignalRServer"];
                var runLocal = string.Equals(runLocalStr, "true", StringComparison.OrdinalIgnoreCase);
                if (!runLocal)
                {
                    Log.Information("RunLocalSignalRServer=false, bỏ qua khởi động SignalRServer local.");
                    return;
                }

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var searchPaths = new[]
                {
                    Path.Combine(baseDir, "SignalRServer", "SignalRServer.exe"),
                    Path.Combine(baseDir, "..", "..", "SignalRServer", "bin", "Release", "net8.0", "publish", "SignalRServer.exe"),
                };

                var exePath = searchPaths.FirstOrDefault(File.Exists);
                if (exePath == null)
                {
                    Log.Warning("Không tìm thấy SignalRServer.exe để tự động khởi động.");
                    return;
                }

                if (Process.GetProcessesByName("SignalRServer").Length > 0)
                {
                    Log.Information("SignalRServer đã đang chạy, bỏ qua.");
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized,
                };
                _signalRServerProcess = Process.Start(psi);
                Log.Information("Đã khởi động SignalRServer từ: {Path}", exePath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Không thể tự động khởi động SignalRServer.");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClinicMS", "logs", "log-.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Ứng dụng ClinicMS đang khởi động...");

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            CultureInfo culture = new CultureInfo("vi-VN");
            culture.DateTimeFormat.AbbreviatedDayNames = new[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);

            StartSignalRServer();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            var signalR = ServiceProvider.GetRequiredService<SignalRClientService>();
            _ = signalR.ConnectAsync();

            SetupExceptionHandling();
        }

    private void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured in appsettings.json.");
        ClinicDbContext.SetDefaultConnectionString(connectionString);

        services.AddDbContext<ClinicDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<CoreDatabaseService>();
        services.AddTransient<AuditLogService>();
        services.AddTransient<MedicineService>();
        services.AddTransient<DoctorService>();
        services.AddTransient<PatientService>();
        services.AddTransient<VisitService>();
        services.AddTransient<ServiceCatalogService>();
        services.AddTransient<InvoiceService>();
        services.AddTransient<DashboardService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<ModalService>();
        services.AddSingleton<ClinicMS.Services.PdfExport.PdfExportService>();
        services.AddSingleton<SignalRClientService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PatientViewModel>();
        services.AddTransient<DoctorViewModel>();
        services.AddTransient<VisitViewModel>();
        services.AddTransient<MedicineViewModel>();
        services.AddTransient<ServiceViewModel>();
        services.AddTransient<InvoiceViewModel>();
        services.AddTransient<AuditLogListViewModel>();

        services.AddSingleton<MainWindow>();
    }

    private void SetupExceptionHandling()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Lỗi UI Thread không xác định");
            MessageBox.Show($"Đã xảy ra lỗi hệ thống: {e.Exception.Message}\n\nChi tiết xem tại file log.",
                "Lỗi Nghiêm Trọng", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Lỗi AppDomain không xác định");
            }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Lỗi Task chạy ngầm không xác định");
            e.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var signalR = ServiceProvider?.GetService<SignalRClientService>();
        signalR?.DisconnectAsync().Wait(TimeSpan.FromSeconds(2));

        if (_signalRServerProcess != null && !_signalRServerProcess.HasExited)
        {
            try { _signalRServerProcess.Kill(); } catch { }
            _signalRServerProcess.Dispose();
            Log.Information("Đã tắt SignalRServer.");
        }

        Log.Information("Ứng dụng đang đóng. Tạm biệt!");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
