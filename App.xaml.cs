using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ClinicMS.Services;
using System.Threading.Tasks;

namespace ClinicMS;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Ép định dạng chuẩn Việt Nam ngay lập tức
        CultureInfo culture = new CultureInfo("vi-VN");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        base.OnStartup(e);

        // 2. HIỆN CỬA SỔ NGAY LẬP TỨC - KHÔNG ĐỢI DATABASE
        var mainWindow = new MainWindow();
        mainWindow.Show();

        // 3. Xử lý các tác vụ nền sau khi UI đã hiện ra
        // DB initialization and settings loading moved to MainViewModel for safety
        // Logic font scale sẽ được cập nhật khi MainViewModel khởi tạo xong

        // Xử lý lỗi tập trung
        DispatcherUnhandledException += (s, ex) => {
            MessageBox.Show($"Lỗi: {ex.Exception.Message}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
