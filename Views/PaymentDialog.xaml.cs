using System;
using System.Windows;
using System.Windows.Controls;
using ClinicMS.ViewModels;
using ClinicMS.Models;
using System.Linq;

namespace ClinicMS.Views;

public partial class PaymentDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Invoice _invoice;
    private double _remaining;

    public PaymentDialog(MainViewModel vm, Invoice invoice)
    {
        InitializeComponent();
        _vm = vm;
        _invoice = invoice;

        double paid = invoice.Payments?.Sum(p => p.Amount) ?? 0;
        _remaining = invoice.TotalAmount - paid;

        TxtRemaining.Text = _remaining.ToString("N0") + " ₫";
        InputAmount.Text = _remaining.ToString("N0");
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _vm.Modal.Close();
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        string raw = InputAmount.Text.Replace(".", "").Replace(",", "").Replace(" ", "");
        if (!double.TryParse(raw, out double amount) || amount <= 0)
        {
            _vm.Toast.ShowError("Số tiền không hợp lệ.");
            return;
        }

        string method = (ComboMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tiền mặt";

        await _vm.InvoiceVM.AddPaymentAsync(_invoice.VisitId, amount, method);
        _vm.Modal.Close();
    }
}
