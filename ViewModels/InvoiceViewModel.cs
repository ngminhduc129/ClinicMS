using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;
using ClinicMS.Services.PdfExport;
using ClinicMS.Views;

namespace ClinicMS.ViewModels;

public class InvoiceViewModel : BaseViewModel
{
    private readonly InvoiceService _invoiceService;
    private readonly PdfExportService _pdfService;
    private readonly ToastService _toast;
    private readonly ModalService _modal;

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

    private string? _dateFilter;
    public string? DateFilter
    {
        get => _dateFilter;
        set { if (SetProperty(ref _dateFilter, value)) { IsTodayFilter = value == DateTime.Today.ToString("yyyy-MM-dd"); _ = LoadAsync(); } }
    }

    private bool _isTodayFilter;
    public bool IsTodayFilter
    {
        get => _isTodayFilter;
        set => SetProperty(ref _isTodayFilter, value);
    }

    public RelayCommand LoadCommand { get; }
    public RelayCommand PrintMedicineCommand { get; }
    public RelayCommand PrintServiceCommand { get; }
    public RelayCommand ShowTodayCommand { get; }
    public RelayCommand ClearDateFilterCommand { get; }

    public InvoiceViewModel(InvoiceService invoiceService, PdfExportService pdfService, ToastService toast, ModalService modal)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _toast = toast;
        _modal = modal;

        LoadCommand = new RelayCommand(async () => await LoadAsync());
        PrintMedicineCommand = new RelayCommand(p => ExportMedicinePdf(p as Invoice));
        PrintServiceCommand = new RelayCommand(p => ExportServicePdf(p as Invoice));
        ShowTodayCommand = new RelayCommand(() => DateFilter = DateTime.Today.ToString("yyyy-MM-dd"));
        ClearDateFilterCommand = new RelayCommand(() => DateFilter = null);

        DateFilter = DateTime.Today.ToString("yyyy-MM-dd");
    }

    public async Task LoadAsync()
    {
        try
        {
            var list = await _invoiceService.GetInvoicesAsync(dateFilter: DateFilter);
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

    private void ExportMedicinePdf(Invoice? inv)
    {
        if (inv == null) return;
        try
        {
            _toast.Show("Đang chuẩn bị hóa đơn thuốc...");
            var path = _pdfService.ExportMedicineInvoice(inv);
            _modal.Show(new PdfPreviewView(path, "HÓA ĐƠN THUỐC", _modal));
            _toast.Show("Đã xuất hóa đơn thuốc thành công");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi xuất PDF: {ex.Message}");
        }
    }

    private void ExportServicePdf(Invoice? inv)
    {
        if (inv == null) return;
        try
        {
            _toast.Show("Đang chuẩn bị hóa đơn dịch vụ...");
            var path = _pdfService.ExportServiceInvoice(inv);
            _modal.Show(new PdfPreviewView(path, "HÓA ĐƠN DỊCH VỤ", _modal));
            _toast.Show("Đã xuất hóa đơn dịch vụ thành công");
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
