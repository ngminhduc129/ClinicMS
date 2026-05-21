using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        if (_vm != null) await _vm.InvoiceVM.LoadAsync();
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
}
