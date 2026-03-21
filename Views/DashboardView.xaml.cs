using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class DashboardView : UserControl
{
    private MainViewModel? _vm;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        TableCard.SizeChanged += (s, e) =>
        {
            TableCard.Clip = new RectangleGeometry(
                new Rect(0, 0, TableCard.ActualWidth, TableCard.ActualHeight),
                18, 18);
        };
    }

    private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            // Nếu VM đã nạp xong data lúc khởi động, UI sẽ tự hiện.
            // Nếu chưa, ta gọi load Visits để Dashboard có data.
            if (_vm.Visits.Count == 0)
            {
                await _vm.LoadVisitsAsync();
            }
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_vm != null) await _vm.LoadVisitsAsync();
    }

    private void Detail_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || (sender as Button)?.DataContext is not Visit v) return;
        
        var form = new VisitFormDialog(_vm, v);
        
        // Customization for "Detail" mode
        form.Loaded += (s, ev) => {
            if (form.FindName("TitleText") is TextBlock title) title.Text = "Chi Tiết Hồ Sơ Khám";
            if (form.FindName("SaveBtn") is Button saveBtn) saveBtn.Visibility = Visibility.Collapsed;
        };
        
        _vm.Modal.Show(form);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || (sender as Button)?.DataContext is not Visit v) return;
        
        var form = new VisitFormDialog(_vm, v);
        _vm.Modal.Show(form, result => {
            if (result == true) _ = _vm.LoadVisitsAsync();
        }, () => form.HasData());
    }
}
