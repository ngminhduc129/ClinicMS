using System.Windows;
using System.Windows.Controls;

namespace ClinicMS.Views;

public partial class InfoRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(InfoRow),
            new PropertyMetadata("", (d, e) => ((InfoRow)d).LabelTb.Text = e.NewValue?.ToString() ?? ""));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(InfoRow),
            new PropertyMetadata("", (d, e) =>
            {
                var row = (InfoRow)d;
                var v = e.NewValue?.ToString();
                row.ValueTb.Text = string.IsNullOrEmpty(v) ? "—" : v;
                row.ValueTb.Foreground = string.IsNullOrEmpty(v)
                    ? System.Windows.Media.Brushes.LightGray
                    : System.Windows.Media.Brushes.DimGray;
            }));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public InfoRow() { InitializeComponent(); }
}
