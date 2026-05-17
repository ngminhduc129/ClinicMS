using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ClinicMS.Services;
using ClinicMS.Views;
using ClinicMS.ViewModels;
using System.Threading.Tasks;
using Serilog;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

using QuestPDF.Infrastructure;

namespace ClinicMS;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 0. Cấu hình QuestPDF (Community License)
        QuestPDF.Settings.License = LicenseType.Community;

        // 1. Khởi tạo Logging
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClinicMS", "logs", "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Ứng dụng ClinicMS đang khởi động...");

        // 1. Cấu hình DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // 2. Ép định dạng chuẩn Việt Nam ngay lập tức
        CultureInfo culture = new CultureInfo("vi-VN");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        base.OnStartup(e);

        // 3. HIỆN CỬA SỔ
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // 4. Xử lý lỗi tập trung (Global Exception Handling)
        SetupExceptionHandling();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<CoreDatabaseService>();
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

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PatientViewModel>();
        services.AddTransient<DoctorViewModel>();
        services.AddTransient<VisitViewModel>();
        services.AddTransient<MedicineViewModel>();
        services.AddTransient<ServiceViewModel>();
        services.AddTransient<InvoiceViewModel>();

        // Windows/Views
        services.AddSingleton<MainWindow>();
    }

    private void SetupExceptionHandling()
    {
        // Lỗi trên UI Thread
        DispatcherUnhandledException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Lỗi UI Thread không xác định");
            MessageBox.Show($"Đã xảy ra lỗi hệ thống: {e.Exception.Message}\n\nChi tiết xem tại file log.", 
                "Lỗi Nghiêm Trọng", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        // Lỗi trên các Thread khác
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Lỗi AppDomain không xác định");
            }
        };

        // Lỗi trong Task chạy ngầm
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Lỗi Task chạy ngầm không xác định");
            e.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Ứng dụng đang đóng. Tạm biệt!");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
