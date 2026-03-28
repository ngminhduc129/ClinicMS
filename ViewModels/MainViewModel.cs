using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
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
    public ObservableCollection<Visit>    TodayVisits { get; } = new();
    public ObservableCollection<Medicine> Medicines { get; } = new();
    public ObservableCollection<Invoice>  Invoices  { get; } = new();
    public ObservableCollection<Appointment> UpcomingAppointments { get; } = new();
    public ObservableCollection<ChartItem> WeeklyStats { get; } = new();

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
        _ = InitializeAndLoad();
    }

    private int _patientOffset = 0;
    private const int PageSize = 50;
    private bool _isAllPatientsLoaded = false;
    private string _lastSearchTerm = "";

    private async Task InitializeAndLoad()
    {
        try 
        {
            await Task.Delay(150);
            await Task.Run(() => DatabaseService.Initialize());
            await LoadSettingsAsync();
            await LoadDashboardDataAsync();
            _ = Task.WhenAll(LoadDoctorsAsync(), LoadMedicinesAsync());
            
            _patientOffset = 0;
            _isAllPatientsLoaded = false;
            await LoadMorePatientsInternalAsync();

            _ = Task.Run(async () => {
                await Task.Delay(2000);
                FullPatientNames = await _db.GetAllPatientNamesAsync();
                FullDoctorNames = await _db.GetAllDoctorNamesAsync();
            });
        } catch { }
    }

    public async Task LoadDashboardDataAsync()
    {
        try {
            TotalPatients = await _db.GetPatientsCountAsync();
            TotalDoctors = await _db.GetDoctorsCountAsync();
            TotalVisits = await _db.GetVisitsCountAsync();
            VisitsToday = await _db.GetTodayVisitsCountAsync();

            var appointments = await _db.GetUpcomingAppointmentsAsync(5);
            UpcomingAppointments.Clear();
            foreach (var a in appointments) UpcomingAppointments.Add(a);

            await LoadWeeklyStatsAsync();

            var todayList = await _db.GetTodayVisitsAsync();
            TodayVisits.Clear();
            foreach (var v in todayList) TodayVisits.Add(v);

            await LoadVisitsAsync();
        } catch { }
    }

    private async Task LoadWeeklyStatsAsync()
    {
        var data = await _db.GetVisitCountByDayAsync(7);
        WeeklyStats.Clear();
        int max = data.Values.Any() ? data.Values.Max() : 1;
        if (max < 5) max = 5;

        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            var dateStr = date.ToString("yyyy-MM-dd");
            int count = data.ContainsKey(dateStr) ? data[dateStr] : 0;
            WeeklyStats.Add(new ChartItem { Label = i == 0 ? "Hôm nay" : date.ToString("dd/MM"), Value = count, Percentage = (double)count / max * 100 });
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var level = await _db.GetSettingAsync("FontSizeLevel");
            if (!string.IsNullOrWhiteSpace(level)) { _fontSizeLevel = level; ApplyFontScale(level, save: false); }
        } catch { }
    }

    private void ApplyFontScale(string level, bool save)
    {
        var scale = level switch { "Small" => 0.85, "Large" => 1.20, _ => 1.00 };
        FontScale = scale;
        FontScaleManager.UpdateApplicationScale(scale);
        if (save) _ = _db.SetSettingAsync("FontSizeLevel", level);
    }

    // ═══════════════════════════════════════════════
    //  PATIENTS & DOCTORS SEARCH/LOAD
    // ═══════════════════════════════════════════════

    public async Task LoadMorePatientsAsync()
    {
        if (!string.IsNullOrEmpty(_lastSearchTerm)) return;
        if (LoadingPatients || _isAllPatientsLoaded) return;
        await LoadMorePatientsInternalAsync();
    }

    private async Task LoadMorePatientsInternalAsync()
    {
        LoadingPatients = true;
        try {
            var nextBatch = await _db.GetPatientsWithContactsAsync(_patientOffset, PageSize);
            if (nextBatch.Count < PageSize) _isAllPatientsLoaded = true;
            foreach (var p in nextBatch) Patients.Add(p);
            _patientOffset += nextBatch.Count;
        } finally { LoadingPatients = false; }
    }

    public async Task SearchPatientsAsync(string term)
    {
        _lastSearchTerm = term;
        _patientOffset = 0; _isAllPatientsLoaded = false;
        LoadingPatients = true;
        try {
            var results = await _db.SearchPatientsAsync(term);
            Patients.Clear();
            foreach (var p in results) Patients.Add(p);
            if (string.IsNullOrEmpty(term)) _patientOffset = Patients.Count;
        } finally { LoadingPatients = false; }
    }

    public async Task SearchDoctorsAsync(string term)
    {
        LoadingDoctors = true;
        try {
            var results = await _db.SearchDoctorsAsync(term);
            Doctors.Clear();
            foreach (var d in results) Doctors.Add(d);
        } finally { LoadingDoctors = false; }
    }

    public async Task LoadPatientsAsync()
    {
        _patientOffset = 0; _isAllPatientsLoaded = false; Patients.Clear();
        await LoadMorePatientsInternalAsync();
        TotalPatients = await _db.GetPatientsCountAsync();
    }

    public async Task LoadDoctorsAsync()
    {
        LoadingDoctors = true;
        try { var list = await _db.GetDoctorsAsync(); Doctors.Clear(); foreach (var d in list) Doctors.Add(d); TotalDoctors = Doctors.Count; }
        catch { } finally { LoadingDoctors = false; }
    }

    public async Task<(Patient?, Contact?)> GetPatientWithContactAsync(int id) => await _db.GetPatientWithContactAsync(id);
    public async Task<List<Visit>> GetPatientVisitsAsync(int patientId) => await _db.GetPatientVisitsAsync(patientId);

    // ═══════════════════════════════════════════════
    //  VISITS & OTHERS
    // ═══════════════════════════════════════════════

    public async Task LoadVisitsAsync()
    {
        if (LoadingVisits) return;
        LoadingVisits = true;
        try 
        { 
            var todayList = await _db.GetTodayVisitsAsync(); 
            Visits.Clear(); 
            foreach (var v in todayList) Visits.Add(v); 

            _ = Task.Run(async () => 
            {
                try 
                {
                    var historyList = await _db.GetVisitsAsync(0, 10000); 
                    App.Current.Dispatcher.Invoke(() => { foreach (var v in historyList) { if (Visits.All(ex => ex.VisitId != v.VisitId)) Visits.Add(v); } });
                } catch { }
            });
        } catch { } finally { LoadingVisits = false; }
    }

    public async Task LoadMedicinesAsync()
    {
        LoadingMedicines = true;
        try { var list = await _db.GetMedicinesAsync(); Medicines.Clear(); foreach (var m in list) Medicines.Add(m); }
        catch { } finally { LoadingMedicines = false; }
    }

    public async Task LoadInvoicesAsync()
    {
        LoadingInvoices = true;
        try { var list = await _db.GetInvoicesAsync(0, 100); Invoices.Clear(); foreach (var i in list) Invoices.Add(i); }
        catch { } finally { LoadingInvoices = false; }
    }

    public async Task<bool> AddVisitAsync(Visit v)
    {
        try { 
            await _db.AddVisitAsync(v); 
            await LoadDashboardDataAsync(); // Nạp lại toàn bộ TodayVisits và Stats
            Toast.Show("Thêm hồ sơ thành công"); 
            return true; 
        }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> UpdateVisitAsync(Visit v)
    {
        try { 
            await _db.UpdateVisitAsync(v); 
            await LoadDashboardDataAsync(); // Nạp lại toàn bộ TodayVisits và Stats
            Toast.Show("Cập nhật thành công"); 
            return true; 
        }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }
    public async Task<bool> DeleteVisitAsync(Visit v)
    {
        try { 
            await _db.DeleteVisitAsync(v.VisitId); 
            await LoadDashboardDataAsync(); // Nạp lại toàn bộ TodayVisits và Stats
            Toast.Show("Đã xóa hồ sơ"); 
            return true; 
        }
        catch (Exception ex) { Toast.ShowError(ex.Message); return false; }
    }

    public async Task<List<PrescriptionDetail>> GetPrescriptionDetailsAsync(int visitId) => await _db.GetPrescriptionDetailsAsync(visitId);
    public async Task SavePrescriptionAsync(int visitId, List<PrescriptionDetail> items) => await _db.SavePrescriptionDetailsAsync(visitId, items);

    public async Task<bool> AddPatientAsync(Patient p) { try { await _db.AddPatientAsync(p); await LoadPatientsAsync(); return true; } catch { return false; } }
    public async Task<bool> UpdatePatientAsync(Patient p) { try { await _db.UpdatePatientAsync(p); await LoadPatientsAsync(); return true; } catch { return false; } }
    public async Task<bool> DeletePatientAsync(Patient p) { try { await _db.DeletePatientAsync(p.PatientId); await LoadPatientsAsync(); return true; } catch { return false; } }
    
    public async Task<bool> AddDoctorAsync(Doctor d) { try { await _db.AddDoctorAsync(d); await LoadDoctorsAsync(); return true; } catch { return false; } }
    public async Task<bool> UpdateDoctorAsync(Doctor d) { try { await _db.UpdateDoctorAsync(d); await LoadDoctorsAsync(); return true; } catch { return false; } }
    public async Task<bool> DeleteDoctorAsync(Doctor d) { try { await _db.DeleteDoctorAsync(d.DoctorId); await LoadDoctorsAsync(); return true; } catch { return false; } }

    public async Task<bool> AddMedicineAsync(Medicine m) { try { await _db.AddMedicineAsync(m); await LoadMedicinesAsync(); return true; } catch { return false; } }
    public async Task<bool> UpdateMedicineAsync(Medicine m) { try { await _db.UpdateMedicineAsync(m); await LoadMedicinesAsync(); return true; } catch { return false; } }
    public async Task<bool> DeleteMedicineAsync(Medicine m) { try { await _db.DeleteMedicineAsync(m.MedicineId); await LoadMedicinesAsync(); return true; } catch { return false; } }
}
