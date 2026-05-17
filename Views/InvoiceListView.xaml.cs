using System;
using System.Windows;
using System.Windows.Controls;
using ClinicMS.ViewModels;
using ClinicMS.Models;

namespace ClinicMS.Views;

public partial class InvoiceListView : UserControl
{
    private MainViewModel? _vm;

    public InvoiceListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
        }
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
