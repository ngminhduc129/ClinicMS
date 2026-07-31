using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class AuditLogListView : UserControl
{
    private MainViewModel? _vm;

    public AuditLogListView()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is MainViewModel vm) _vm = vm;
        };

        AuditLogDataGrid.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));

        TableCard.SizeChanged += (s, e) =>
        {
            TableCard.Clip = new RectangleGeometry(
                new Rect(0, 0, TableCard.ActualWidth, TableCard.ActualHeight),
                18, 18);
        };
    }

    private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        try
        {
            if (e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
            {
                if (_vm != null)
                    await _vm.AuditLogVM.LoadMoreAsync();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in AuditLogListView.OnScrollChanged");
        }
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm != null) await _vm.AuditLogVM.SearchAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in AuditLogListView.Search_Click");
            _vm?.Toast.ShowError($"Lỗi tìm kiếm: {ex.Message}");
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm != null) await _vm.AuditLogVM.LoadAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in AuditLogListView.Refresh_Click");
            _vm?.Toast.ShowError($"Lỗi làm mới: {ex.Message}");
        }
    }
}
