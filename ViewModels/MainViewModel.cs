using System.Collections.ObjectModel;
using System.IO;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly DatabaseService _db = new();
    public ToastService Toast { get; } = new();
    public ModalService Modal { get; } = new();

    private string _fontSizeLevel = "Medium";
    public string FontSizeLevel
    {
        get => _fontSizeLevel;
        set
        {
            if (!SetProperty(ref _fontSizeLevel, value)) return;
            ApplyFontScale(value, save: true);
        }
    }

    private double _fontScale = 1.0;
    public double FontScale
    {
        get => _fontScale;
        private set => SetProperty(ref _fontScale, value);
    }

    private string _currentPage = "dashboard";
    public string CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(IsPageDashboard));
                OnPropertyChanged(nameof(IsPagePatients));
                OnPropertyChanged(nameof(IsPageDoctors));
                OnPropertyChanged(nameof(IsPageVisits));
                OnPropertyChanged(nameof(IsPageMedicines));
                OnPropertyChanged(nameof(IsPageInvoices));
                OnPropertyChanged(nameof(IsPageSettings));

                // Lazy load data based on page
                _ = value switch
                {
                    "patients"  => Patients.Count == 0 ? LoadPatientsAsync() : Task.CompletedTask,
                    "doctors"   => Doctors.Count == 0 ? LoadDoctorsAsync() : Task.CompletedTask,
                    "visits"    => Visits.Count == 0 ? LoadVisitsAsync() : Task.CompletedTask,
                    "medicines" => Medicines.Count == 0 ? LoadMedicinesAsync() : Task.CompletedTask,
                    "invoices"  => Invoices.Count == 0 ? LoadInvoicesAsync() : Task.CompletedTask,
                    "dashboard" => LoadDashboardDataAsync(),
                    _           => Task.CompletedTask
                };
            }
        }
    }
    public bool IsPageDashboard => CurrentPage == "dashboard";
    public bool IsPagePatients  => CurrentPage == "patients";
    public bool IsPageDoctors   => CurrentPage == "doctors";
    public bool IsPageVisits    => CurrentPage == "visits";
    public bool IsPageMedicines => CurrentPage == "medicines";
    public bool IsPageInvoices  => CurrentPage == "invoices";
    public bool IsPageSettings  => CurrentPage == "settings";

    public ObservableCollection<Patient>  Patients  { get; } = new();
    public ObservableCollection<Doctor>   Doctors   { get; } = new();
    public ObservableCollection<Visit>    Visits    { get; } = new();
    public ObservableCollection<Medicine> Medicines { get; } = new();
    public ObservableCollection<Invoice>  Invoices  { get; } = new();

    // Cache cho gợi ý tìm kiếm (Suggestion)
    private List<string> _fullPatientNames = new();
    public List<string> FullPatientNames { get => _fullPatientNames; set => SetProperty(ref _fullPatientNames, value); }
    
    private List<string> _fullDoctorNames = new();
    public List<string> FullDoctorNames { get => _fullDoctorNames; set => SetProperty(ref _fullDoctorNames, value); }

    private bool _loadingPatients;  public bool LoadingPatients  { get => _loadingPatients;  set => SetProperty(ref _loadingPatients,  value); }
    private bool _loadingDoctors;   public bool LoadingDoctors   { get => _loadingDoctors;   set => SetProperty(ref _loadingDoctors,   value); }
    private bool _loadingVisits;    public bool LoadingVisits    { get => _loadingVisits;    set => SetProperty(ref _loadingVisits,    value); }
    private bool _loadingMedicines; public bool LoadingMedicines { get => _loadingMedicines; set => SetProperty(ref _loadingMedicines, value); }
    private bool _loadingInvoices;  public bool LoadingInvoices  { get => _loadingInvoices;  set => SetProperty(ref _loadingInvoices,  value); }

    private int _totalPatients; public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }        
    private int _totalDoctors;  public int TotalDoctors  { get => _totalDoctors;  set => SetProperty(ref _totalDoctors,  value); }        
    private int _totalVisits;   public int TotalVisits   { get => _totalVisits;   set => SetProperty(ref _totalVisits,   value); }        
    private int _visitsToday;   public int VisitsToday   { get => _visitsToday;   set => SetProperty(ref _visitsToday,   value); }        

    public RelayCommand NavigateCommand { get; }

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand(p => CurrentPage = p?.ToString() ?? "dashboard");
        
        // Khởi tạo dữ liệu theo quy trình an toàn
        _ = InitializeAndLoad();
    }

    private int _patientOffset = 0;
    private const int PageSize = 50;
    private bool _isAllPatientsLoaded = false;

    private async Task InitializeAndLoad()
    {
        string logFile = @"D:\ClinicMS\boot_log.txt";
        try 
        {
            File.WriteAllText(logFile, $"[{DateTime.Now}] Starting boot with Lazy Loading...\n");

            await Task.Run(() => DatabaseService.Initialize());
            await LoadSettingsAsync();
            await LoadDashboardDataAsync();
            
            // Nạp cache tên đầy đủ trước để dùng cho tìm kiếm (Suggestion)
            FullPatientNames = await _db.GetAllPatientNamesAsync();
            FullDoctorNames = await _db.GetAllDoctorNamesAsync();
            
            await Task.WhenAll(LoadDoctorsAsync(), LoadMedicinesAsync());

            // Chỉ nạp 50 người đầu tiên cho danh sách hiển thị
            _patientOffset = 0;
            _isAllPatientsLoaded = false;
            Patients.Clear();
            await LoadMorePatientsInternalAsync();

            File.AppendAllText(logFile, $"[{DateTime.Now}] Initial batch loaded. Count: {Patients.Count}\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR: {ex}\n");
        }
    }

    public async Task LoadMorePatientsAsync()
    {
        // Nếu đang tìm kiếm thì không thực hiện LoadMore theo phân trang bình thường
        if (!string.IsNullOrEmpty(_lastSearchTerm)) return;
        
        if (LoadingPatients || _isAllPatientsLoaded) return;
        await LoadMorePatientsInternalAsync();
    }

    private string _lastSearchTerm = "";

    public async Task SearchPatientsAsync(string term)
    {
        _lastSearchTerm = term;
        _patientOffset = 0;
        _isAllPatientsLoaded = false;
        
        LoadingPatients = true;
        try
        {
            var results = await _db.SearchPatientsAsync(term);
            Patients.Clear();
            foreach (var p in results) Patients.Add(p);
            
            // Nếu không tìm kiếm, ta coi như đã nạp xong trang đầu
            if (string.IsNullOrEmpty(term)) _patientOffset = Patients.Count;
        }
        finally
        {
            LoadingPatients = false;
        }
    }

    private string _lastDoctorSearch = "";
    public async Task SearchDoctorsAsync(string term)
    {
        _lastDoctorSearch = term;
        LoadingDoctors = true;
        try
        {
            var results = await _db.SearchDoctorsAsync(term);
            Doctors.Clear();
            foreach (var d in results) Doctors.Add(d);
        }
        finally { LoadingDoctors = false; }
    }

    private async Task LoadMorePatientsInternalAsync()
    {
        LoadingPatients = true;
        try
        {
            var nextBatch = await _db.GetPatientsWithContactsAsync(_patientOffset, PageSize);
            if (nextBatch.Count < PageSize) _isAllPatientsLoaded = true;

            foreach (var p in nextBatch) Patients.Add(p);
            _patientOffset += nextBatch.Count;
            OnPropertyChanged(nameof(Patients));
        }
        finally
        {
            LoadingPatients = false;
        }
    }


    private async Task LoadAllDataAsync()
    {
        // Method này đã được gộp vào InitializeAndLoadAsync
        await Task.CompletedTask;
    }

    private async Task LoadInitialPatientsAsync()
    {
        LoadingPatients = true;
        try
        {
            var list = await _db.GetPatientsWithContactsAsync(0, 50);
            Patients.Clear();
            foreach (var p in list) Patients.Add(p);
            TotalPatients = await _db.GetPatientsCountAsync();
        }
        catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); }
        finally { LoadingPatients = false; }
    }

    private async Task LoadDashboardDataAsync()
    {
        try {
            await LoadVisitsAsync();
            // Lấy số lượng nhanh từ DB thay vì đếm trong bộ nhớ
            TotalPatients = await _db.GetPatientsCountAsync();
            var dList = await _db.GetDoctorsAsync();
            TotalDoctors = dList.Count;
        } catch { }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var level = await _db.GetSettingAsync("FontSizeLevel");
            if (!string.IsNullOrWhiteSpace(level))
            {
                _fontSizeLevel = level;
                OnPropertyChanged(nameof(FontSizeLevel));
                ApplyFontScale(level, save: false);
                return;
            }
        }
        catch
        {
            // ignore settings errors
        }

        ApplyFontScale(_fontSizeLevel, save: false);
    }

    private void ApplyFontScale(string level, bool save)
    {
        var scale = level switch
        {
            "Small" => 0.85,
            "Large" => 1.20,
            _ => 1.00
        };

        FontScale = scale;
        // Sử dụng manager mới để cập nhật toàn cục
        FontScaleManager.UpdateApplicationScale(scale);

        if (save)
        {
            _ = _db.SetSettingAsync("FontSizeLevel", level);
        }
    }

    private async Task LoadAllAsync()
    {
        // Đã thay thế bằng LoadDashboardDataAsync và Lazy Loading
        await LoadDashboardDataAsync();
    }

    public async Task LoadPatientsAsync()
    {
        LoadingPatients = true;
        try
        {
            var list = await _db.GetPatientsWithContactsAsync();

            Patients.Clear();
            foreach (var p in list) Patients.Add(p);
            TotalPatients = await _db.GetPatientsCountAsync();
        }
        catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); }
        finally { LoadingPatients = false; }
    }

    public async Task LoadDoctorsAsync()
    {
        LoadingDoctors = true;
        try { var list = await _db.GetDoctorsAsync(); Doctors.Clear(); foreach (var d in list) Doctors.Add(d); TotalDoctors = Doctors.Count; }
        catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); } finally { LoadingDoctors = false; }
    }

    public async Task LoadVisitsAsync()
    {
        LoadingVisits = true;
        try { 
            var list = await _db.GetVisitsAsync(); Visits.Clear(); foreach (var v in list) Visits.Add(v); TotalVisits = Visits.Count;
            var today = DateTime.Today.ToString("yyyy-MM-dd"); VisitsToday = Visits.Count(v => v.VisitTime?.StartsWith(today) == true);
        }
        catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); } finally { LoadingVisits = false; }
    }

    public async Task LoadMedicinesAsync()
    {
        LoadingMedicines = true;
        try { var list = await _db.GetMedicinesAsync(); Medicines.Clear(); foreach (var m in list) Medicines.Add(m); }
        catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); } finally { LoadingMedicines = false; }
    }

    public async Task LoadInvoicesAsync()
    {
        LoadingInvoices = true;
        try { var list = await _db.GetInvoicesAsync(); Invoices.Clear(); foreach (var i in list) Invoices.Add(i); }
        catch (Exception ex) { Toast.ShowError($"Lỗi: {ex.Message}"); } finally { LoadingInvoices = false; }
    }

    public async Task<bool> AddPatientAsync(Patient p)
    {
        try { await _db.AddPatientAsync(p); await LoadPatientsAsync(); Toast.Show("Thêm bệnh nhân thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> UpdatePatientAsync(Patient p)
    {
        try { await _db.UpdatePatientAsync(p); await LoadPatientsAsync(); Toast.Show("Cập nhật bệnh nhân thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> DeletePatientAsync(Patient p)
    {
        try { await _db.DeletePatientAsync(p.PatientId); await LoadPatientsAsync(); Toast.Show("Xóa bệnh nhân thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<(Patient?, Contact?)> GetPatientWithContactAsync(int id) => await _db.GetPatientWithContactAsync(id);
    public async Task<List<Visit>> GetPatientVisitsAsync(int patientId) => await _db.GetPatientVisitsAsync(patientId);

    public async Task<bool> AddDoctorAsync(Doctor d)
    {
        try { await _db.AddDoctorAsync(d); await LoadDoctorsAsync(); Toast.Show("Thêm bác sĩ thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> UpdateDoctorAsync(Doctor d)
    {
        try { await _db.UpdateDoctorAsync(d); await LoadDoctorsAsync(); Toast.Show("Cập nhật bác sĩ thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> DeleteDoctorAsync(Doctor d)
    {
        try { await _db.DeleteDoctorAsync(d.DoctorId); await LoadDoctorsAsync(); Toast.Show("Xóa bác sĩ thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }

    public async Task<bool> AddVisitAsync(Visit v)
    {
        try { await _db.AddVisitAsync(v); await LoadVisitsAsync(); Toast.Show("Thêm hồ sơ khám thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> UpdateVisitAsync(Visit v)
    {
        try { await _db.UpdateVisitAsync(v); await LoadVisitsAsync(); Toast.Show("Cập nhật hồ sơ khám thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> DeleteVisitAsync(Visit v)
    {
        try { await _db.DeleteVisitAsync(v.VisitId); await LoadVisitsAsync(); Toast.Show("Xóa hồ sơ khám thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }

    public async Task<bool> AddMedicineAsync(Medicine m)
    {
        try { await _db.AddMedicineAsync(m); await LoadMedicinesAsync(); Toast.Show("Thêm thuốc thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> UpdateMedicineAsync(Medicine m)
    {
        try { await _db.UpdateMedicineAsync(m); await LoadMedicinesAsync(); Toast.Show("Cập nhật thuốc thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> DeleteMedicineAsync(Medicine m)
    {
        try { await _db.DeleteMedicineAsync(m.MedicineId); await LoadMedicinesAsync(); Toast.Show("Xóa thuốc thành công"); return true; }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
}
