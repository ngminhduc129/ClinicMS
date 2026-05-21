using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using ClinicMS.Models;
using ClinicMS.Services;
using ClinicMS.Services.PdfExport;

namespace ClinicMS.ViewModels;

public class VisitViewModel : BaseViewModel
{
    private readonly VisitService _visitService;
    private readonly InvoiceService _invoiceService;
    private readonly ToastService _toast;
    private readonly PdfExportService _pdf;
    private readonly ModalService _modal;

    public ObservableCollection<Visit> Visits { get; } = new();
    
    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    private int _offset = 0;
    private int _totalCount;
    private bool _hasMore;
    public bool HasMore { get => _hasMore; set => SetProperty(ref _hasMore, value); }

    private string _searchTerm = "";
    public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }

    private string? _dateFilter = null;
    public string? DateFilter { get => _dateFilter; set => SetProperty(ref _dateFilter, value); }
    public string? VisitDateFilter { get => DateFilter; set => DateFilter = value; } 

    private const int PageSize = 50;

    public RelayCommand ExportPrescriptionCommand { get; }
    public RelayCommand ExportMedicalRecordCommand { get; }

    public VisitViewModel(VisitService visitService, InvoiceService invoiceService, ToastService toast, PdfExportService pdf, ModalService modal)
    {
        _visitService = visitService;
        _invoiceService = invoiceService;
        _toast = toast;
        _pdf = pdf;
        _modal = modal;

        ExportPrescriptionCommand = new RelayCommand(async p => await DoExportPdf(p, "prescription"));
        ExportMedicalRecordCommand = new RelayCommand(async p => await DoExportPdf(p, "medical_record"));
    }

    public async Task<List<PrescriptionDetail>> GetPrescriptionDetailsAsync(int visitId) => await _visitService.GetPrescriptionDetailsAsync(visitId);
    public async Task SavePrescriptionAsync(int visitId, List<PrescriptionDetail> details) 
    { 
        await _visitService.SavePrescriptionDetailsAsync(visitId, details);
        await _invoiceService.GenerateOrUpdateInvoiceAsync(visitId);
    }

    public async Task<List<ServiceOrder>> GetServiceOrdersAsync(int visitId) => await _visitService.GetServiceOrdersAsync(visitId);
    public async Task SaveServiceOrdersAsync(int visitId, List<ServiceOrder> orders) 
    { 
        await _visitService.SaveServiceOrdersAsync(visitId, orders);
        await _invoiceService.GenerateOrUpdateInvoiceAsync(visitId);
    }

    private async Task DoExportPdf(object? p, string type)
    {
        if (p is not Visit v) return;
        
        try
        {
            _toast.Show("Đang chuẩn bị file PDF...");
            
            // Nạp đầy đủ dữ liệu trước khi in
            // Đảm bảo nạp lại từ DB để có đầy đủ các liên kết (Include)
            var fullVisit = await _visitService.GetVisitDetailsAsync(v.VisitId);
            if (fullVisit == null) {
                _toast.ShowError("Không tìm thấy dữ liệu hồ sơ khám.");
                return;
            }

            string filePath;
            string title;
            if (type == "prescription") {
                filePath = _pdf.ExportPrescription(fullVisit);
                title = "Đơn Thuốc";
            }
            else {
                filePath = _pdf.ExportMedicalRecord(fullVisit);
                title = "Bệnh Án";
            }
            
            _modal.Show(new ClinicMS.Views.PdfPreviewView(filePath, title, _modal));
            
            _toast.Show("Đã xuất file PDF thành công");
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi xuất PDF: {ex.Message}");
        }
    }

    public async Task LoadAsync()
    {
        if (Loading) return;
        Loading = true;
        try 
        {
            if (_offset == 0)
            {
                Visits.Clear();
                _totalCount = await _visitService.GetVisitsSearchCountAsync(SearchTerm, DateFilter);
            }

            var results = await _visitService.SearchVisitsAsync(SearchTerm, _offset, PageSize, DateFilter);
            foreach (var v in results) Visits.Add(v);
            
            _offset += results.Count;
            HasMore = Visits.Count < _totalCount;
        } 
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải danh sách hồ sơ khám: {ex.Message}");
        } 
        finally { Loading = false; }
    }

    public async Task SearchAsync(string term)
    {
        SearchTerm = term;
        _offset = 0;
        await LoadAsync();
    }

    public async Task SearchByDateAsync(string? date)
    {
        DateFilter = date;
        _offset = 0;
        await LoadAsync();
    }

    public async Task LoadMoreAsync()
    {
        if (Loading || !HasMore) return;
        await LoadAsync();
    }

    public event Action? DataChanged;

    public async Task<bool> AddAsync(Visit v)
    {
        try 
        { 
            await _visitService.AddVisitAsync(v); 
            await _invoiceService.GenerateOrUpdateInvoiceAsync(v.VisitId);
            _offset = 0; await LoadAsync(); _toast.Show("Thêm hồ sơ thành công"); DataChanged?.Invoke(); return true; 
        }
        catch (Exception ex) { _toast.ShowError(ex.Message); return false; }
    }

    public async Task<bool> UpdateAsync(Visit v)
    {
        try 
        { 
            await _visitService.UpdateVisitAsync(v); 
            await _invoiceService.GenerateOrUpdateInvoiceAsync(v.VisitId);
            _offset = 0; await LoadAsync(); _toast.Show("Cập nhật thành công"); DataChanged?.Invoke(); return true; 
        }
        catch (Exception ex) { _toast.ShowError(ex.Message); return false; }
    }

    public async Task<bool> DeleteAsync(Visit v)
    {
        try { await _visitService.DeleteVisitAsync(v.VisitId); _offset = 0; await LoadAsync(); _toast.Show("Đã xóa hồ sơ"); DataChanged?.Invoke(); return true; }
        catch (Exception ex) { _toast.ShowError(ex.Message); return false; }
    }
}
