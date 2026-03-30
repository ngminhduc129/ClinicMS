using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ClinicMS.Services;

namespace ClinicMS.Converters;

/// <summary>true → Visible, false → Collapsed</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility.Visible;
}

/// <summary>true → Collapsed, false → Visible (inverted)</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is not Visibility.Visible;
}

/// <summary>null/empty → Visible, else Collapsed</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (value == null || string.IsNullOrEmpty(value.ToString())) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>null/empty → Collapsed, else Visible</summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (value == null || string.IsNullOrEmpty(value?.ToString())) ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>page key + active key → FontWeight (Bold vs Normal)</summary>
public class PageToBoldConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values.Length == 2 && values[0]?.ToString() == values[1]?.ToString())
            return FontWeights.Bold;
        return FontWeights.Normal;
    }
    public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => Array.Empty<object>();
}

/// <summary>Status string → brush color</summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        return value?.ToString() switch
        {
            "Waiting" or "Đang chờ" => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
            "Completed" or "Hoàn thành" => new SolidColorBrush(Color.FromRgb(5, 150, 105)),
            _ => new SolidColorBrush(Color.FromRgb(37, 99, 235))
        };
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>Status string → background color (light)</summary>
public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        return value?.ToString() switch
        {
            "Waiting" or "Đang chờ" => new SolidColorBrush(Color.FromRgb(254, 243, 199)),
            "Completed" or "Hoàn thành" => new SolidColorBrush(Color.FromRgb(209, 250, 229)),
            _ => new SolidColorBrush(Color.FromRgb(219, 234, 254))
        };
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>Toast type → background brush</summary>
public class ToastTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is ToastType type && type == ToastType.Error
            ? new SolidColorBrush(Color.FromRgb(254, 226, 226))
            : new SolidColorBrush(Color.FromRgb(220, 252, 231));
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>Toast type → text brush</summary>
public class ToastTypeToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is ToastType type && type == ToastType.Error
            ? new SolidColorBrush(Color.FromRgb(185, 28, 28))
            : new SolidColorBrush(Color.FromRgb(22, 101, 52));
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>int count == 0 → Visible</summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}

/// <summary>Hỗ trợ hiển thị số thứ tự trong DataGrid</summary>
public class IndexConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values.Length < 2) return "";
        var item = values[0];
        var list = values[1] as System.Collections.IEnumerable;
        if (item == null || list == null) return "";

        int index = 0;
        foreach (var i in list)
        {
            if (ReferenceEquals(i, item)) return (index + 1).ToString();
            index++;
        }
        return "";
    }
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Tính toán chiều cao cột biểu đồ dựa trên % và chiều cao tối đa</summary>
public class PercentageToHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type t, object p, CultureInfo c)
    {
        if (values.Length < 2) return 0.0;
        double percentage = System.Convert.ToDouble(values[0]);
        double maxHeight = System.Convert.ToDouble(values[1]);
        return (percentage / 100.0) * maxHeight;
    }
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Nhân cỡ chữ gốc với hệ số FontScale (để scale cả các cỡ chữ cứng)</summary>
public class FontSizeConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        double baseSize = 14.0;
        if (p != null) double.TryParse(p.ToString(), out baseSize);
        else if (value is double d) baseSize = d;

        // Lấy tỉ lệ từ Resource (FontScaleManager đã cập nhật)
        double scale = 1.0;
        if (Application.Current != null && Application.Current.Resources.Contains("FontScale"))
            scale = (double)Application.Current.Resources["FontScale"];

        return baseSize * scale;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => DependencyProperty.UnsetValue;
}
