using System.Windows;
using System.Windows.Media;

namespace ClinicMS;

public static class FontScaleManager
{
    // Cập nhật tất cả các Resource Font Size dựa trên hệ số tỉ lệ
    public static void UpdateApplicationScale(double scale)
    {
        if (Application.Current != null)
        {
            var res = Application.Current.Resources;

            // Cập nhật các giá trị FontSize dựa trên kích thước gốc trong ThemeTypography.xaml
            // Sử dụng DynamicResource trong XAML sẽ giúp UI cập nhật ngay lập tức
            res["FontSizePageTitle"]       = 30.0 * scale;
            res["FontSizeSectionTitle"]    = 22.0 * scale;
            res["FontSizeSubsectionTitle"] = 19.0 * scale;
            res["FontSizeCardTitle"]       = 17.0 * scale;
            res["FontSizeBody"]            = 15.0 * scale;
            res["FontSizeSmall"]           = 14.0 * scale;
            res["FontSizeXSmall"]          = 13.0 * scale;
            res["FontSizeButton"]          = 15.0 * scale;
            res["FontSizeLabel"]           = 14.0 * scale;
            res["FontSizeHelper"]          = 13.0 * scale;

            // Lưu hệ số scale chung để dùng cho các mục đích khác nếu cần
            res["FontScale"] = scale;
        }
    }

    public static void ApplyScaleToWindow(Window window, double scale)
    {
        // Không còn cần thiết nếu dùng DynamicResource cho FontSize,
        // trừ khi muốn scale toàn bộ một cửa sổ popup cụ thể.
    }
}
