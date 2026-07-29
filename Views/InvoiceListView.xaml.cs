using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

using ClinicMS.ViewModels;
using ClinicMS.Models;

namespace ClinicMS.Views;

public partial class InvoiceListView : UserControl, INotifyPropertyChanged
{
    private MainViewModel? _vm;

    public ObservableCollection<InvoiceItem> MedicineItems { get; } = new();
    public ObservableCollection<InvoiceItem> ServiceItems { get; } = new();

    public string MedicineTotalDisplay => MedicineItems.Sum(i => i.Subtotal).ToString("N0") + " đ";
    public string ServiceTotalDisplay => ServiceItems.Sum(i => i.Subtotal).ToString("N0") + " đ";

    public event PropertyChangedEventHandler? PropertyChanged;

    public InvoiceListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.InvoiceVM.PropertyChanged -= OnInvoiceVMPropertyChanged;
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            _vm.InvoiceVM.PropertyChanged += OnInvoiceVMPropertyChanged;
            UpdateFilteredItems();
        }
    }

    private void OnInvoiceVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InvoiceViewModel.SelectedInvoice))
            UpdateFilteredItems();
    }

    private void UpdateFilteredItems()
    {
        MedicineItems.Clear();
        ServiceItems.Clear();

        var items = _vm?.InvoiceVM.SelectedInvoice?.Items;
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.ItemType == "Medicine")
                    MedicineItems.Add(item);
                else
                    ServiceItems.Add(item);
            }
        }

        MedicineSection.Visibility = MedicineItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        ServiceSection.Visibility = ServiceItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MedicineItems)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServiceItems)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MedicineTotalDisplay)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServiceTotalDisplay)));
    }

    private async void Refresh_Click(object s, RoutedEventArgs e)
    {
        try
        {
            if (_vm != null) await _vm.InvoiceVM.LoadAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in InvoiceListView.Refresh_Click");
            _vm?.Toast.ShowError($"Lỗi khi làm mới: {ex.Message}");
        }
    }

    private void Filter_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || s is not RadioButton rb) return;
        _vm.InvoiceVM.StatusFilter = rb.Tag?.ToString() ?? "All";
    }

    private void Pay_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || _vm.InvoiceVM.SelectedInvoice == null) return;

        var inv = _vm.InvoiceVM.SelectedInvoice;
        _vm.Modal.Show(new PaymentDialog(_vm, inv));
    }

    private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null) return;
        if (sender is DatePicker dp && dp.SelectedDate.HasValue)
        {
            _vm.InvoiceVM.DateFilter = dp.SelectedDate.Value.ToString("yyyy-MM-dd");
        }
    }

    private void DatePicker_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker dp) return;
        SetWatermark(dp);
    }

    private static void SetWatermark(DatePicker dp)
    {
        var textBox = FindVisualChild<DatePickerTextBox>(dp);
        if (textBox == null) return;

        var watermark = FindVisualChildByName<TextBlock>(textBox, "PART_Watermark")
                     ?? FindVisualChildByName<TextBlock>(textBox, "Watermark")
                     ?? FindVisualChild<TextBlock>(textBox);
        if (watermark == null) return;

        watermark.Text = "Chọn ngày...";
        watermark.Foreground = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
    }

    private static T? FindVisualChildByName<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name && fe is T t)
                return t;
            var result = FindVisualChildByName<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }
}
