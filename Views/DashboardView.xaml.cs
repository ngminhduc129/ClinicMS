using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class DashboardView : UserControl
{
    private MainViewModel? _vm;
    private ScrollViewer? _internalScrollViewer;
    private double _targetOffset = 0;
    private DateTime _lastWheelTime = DateTime.MinValue;

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
        
        DashboardDataGrid.PreviewMouseWheel += DashboardDataGrid_PreviewMouseWheel;
    }

    private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            if (_vm.Visits.Count == 0) await _vm.LoadVisitsAsync();
        }
    }

    private void DashboardDataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (_internalScrollViewer == null)
        {
            _internalScrollViewer = FindVisualChild<ScrollViewer>(DashboardDataGrid);
        }

        if (_internalScrollViewer == null) return;

        DateTime now = DateTime.Now;
        double elapsed = (now - _lastWheelTime).TotalMilliseconds;
        _lastWheelTime = now;

        // Cơ chế gia tốc chuẩn Desktop hiện đại:
        // Càng lăn nhanh, lực đẩy càng mạnh. Tối đa x4.5 lần tốc độ cơ bản.
        double multiplier = 1.0;
        if (elapsed > 0 && elapsed < 120)
        {
            multiplier = Math.Min(4.5, 1.0 + (120 - elapsed) / 25.0); 
        }

        double currentOffset = _internalScrollViewer.VerticalOffset;
        
        // Luôn tính toán đích đến dựa trên vị trí hiện tại của ScrollViewer 
        // để đảm bảo không có hiện tượng "trôi" ngoài ý muốn khi đổi hướng.
        if (elapsed > 200 || (_targetOffset > currentOffset && e.Delta > 0) || (_targetOffset < currentOffset && e.Delta < 0))
        {
            _targetOffset = currentOffset;
        }

        _targetOffset -= e.Delta * multiplier;

        // Giới hạn biên an toàn
        if (_targetOffset < 0) _targetOffset = 0;
        if (_targetOffset > _internalScrollViewer.ScrollableHeight) _targetOffset = _internalScrollViewer.ScrollableHeight;

        // Hoạt ảnh 350ms với QuarticEase: Tiêu chuẩn của Modern Desktop
        DoubleAnimation animation = new DoubleAnimation
        {
            To = _targetOffset,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
        };

        // SnapshotAndReplace giúp cộng dồn lực mượt mà không gây khựng lag
        _internalScrollViewer.BeginAnimation(ScrollViewerHelper.VerticalOffsetProperty, animation, HandoffBehavior.SnapshotAndReplace);
        
        e.Handled = true;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_vm != null) await _vm.LoadVisitsAsync();
    }

    private void Detail_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || (sender as Button)?.DataContext is not Visit v) return;
        var form = new VisitFormDialog(_vm, v);
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

    private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t) return t;
            T? childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null) return childOfChild;
        }
        return null;
    }
}

public static class ScrollViewerHelper
{
    public static readonly DependencyProperty VerticalOffsetProperty =
        DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerHelper),
            new PropertyMetadata(0.0, OnVerticalOffsetChanged));

    public static void SetVerticalOffset(DependencyObject target, double value) => target.SetValue(VerticalOffsetProperty, value);
    public static double GetVerticalOffset(DependencyObject target) => (double)target.GetValue(VerticalOffsetProperty);

    private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}
