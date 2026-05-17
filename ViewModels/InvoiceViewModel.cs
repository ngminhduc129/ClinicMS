using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;
using ClinicMS.Services.PdfExport;

namespace ClinicMS.ViewModels;

public class InvoiceViewModel : BaseViewModel
{
    private readonly InvoiceService _invoiceService;
    private readonly PdfExportService _pdfService;
    private readonly ToastService _toast;

    public ObservableCollection<Invoice> Invoices { get; } = new();

    private Invoice? _selectedInvoice;
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set => SetProperty(ref _selectedInvoice, value);
    }

    private string _statusFilter = "All";
    public string StatusFilter
    {
        get => _statusFilter;
        set { if (SetProperty(ref _statusFilter, value)) _ = LoadAsync(); }
    }

    public RelayCommand LoadCommand { get; }
    public RelayCommand PrintCommand { get; }

    public InvoiceViewModel(InvoiceService invoiceService, PdfExportService pdfService, ToastService toast)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _toast = toast;

        LoadCommand = new RelayCommand(async () => await LoadAsync());
        PrintCommand = new RelayCommand(p => ExportPdf(p as Invoice));
    }

    public async Task LoadAsync()
    {
        try
        {
            var list = await _invoiceService.GetInvoicesAsync();
            Invoices.Clear();

            var filtered = StatusFilter switch
            {
                "Unpaid" => list.Where(i => i.Status == "Unpaid" || i.Status == "Partial"),
                "Paid" => list.Where(i => i.Status == "Paid"),
                _ => list
            };

            foreach (var inv in filtered) Invoices.Add(inv);
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi nạp hóa đơn: {ex.Message}");
        }
    }

    private void ExportPdf(Invoice? inv)
    {
        if (inv == null) return;
        try
        {
            var path = _pdfService.ExportInvoice(inv);
            // Mở file (PdfExportService có hàm OpenFile nhưng nó private, tôi sẽ gọi qua shell hoặc sửa PdfExportService sau)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            _toast.Show("Đã xuất hóa đơn PDF");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi xuất PDF: {ex.Message}");
        }
    }

    public async Task AddPaymentAsync(int visitId, double amount, string method)
    {
        try
        {
            var payment = new Payment
            {
                VisitId = visitId,
                Amount = amount,
                PaymentMethod = method,
                PaymentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            await _invoiceService.AddPaymentAsync(payment);
            await LoadAsync();
            if (SelectedInvoice != null && SelectedInvoice.VisitId == visitId)
            {
                SelectedInvoice = await _invoiceService.GetInvoiceByVisitIdAsync(visitId);
            }
            _toast.Show("Đã ghi nhận thanh toán");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi thanh toán: {ex.Message}");
        }
    }
}
