using System.Windows;
using System.Windows.Media;

namespace ClinicMS;

public static class FontScaleManager
{
    // 1. Khai báo Resource Key để dùng trong XAML
    public const string FontScaleResourceKey = "FontScale";

    // 2. Cập nhật Resource và ép buộc cập nhật UI
    public static void UpdateApplicationScale(double scale)
    {
        if (Application.Current != null)
        {
            // Cập nhật Resource để các cửa sổ mới mở ra sẽ nhận giá trị này
            Application.Current.Resources[FontScaleResourceKey] = scale;
            
            // LƯU Ý: Không duyệt qua các cửa sổ và gọi ApplyScaleToWindow vì:
            // 1. MainWindow đã có binding trực tiếp tới FontScale trong XAML.
            // 2. Việc gọi ApplyScaleToWindow ở đây sẽ gây ra hiện tượng phóng to gấp đôi (Double Scaling).
        }
    }

    public static void ApplyScaleToWindow(Window window, double scale)
    {
        if (window == null) return;

        // Nếu là MainWindow, chúng ta có thể muốn target vào một element cụ thể thay vì toàn bộ window (bao gồm cả Sidebar)
        // Nhưng để đơn giản và đồng bộ, áp dụng LayoutTransform lên Content của Window là cách tốt nhất.
        
        if (window.Content is FrameworkElement root)
        {
            // Đảm bảo dùng LayoutTransform để không bị cắt xén (clip) như RenderTransform
            root.LayoutTransform = new ScaleTransform(scale, scale);
        }
    }
}
