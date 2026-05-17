using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class MainViewModel : BaseViewModel
{
    public DatabaseService Db { get; }
    private readonly CoreDatabaseService _coreDb;
    public ToastService Toast { get; }
    public ModalService Modal { get; }

    // Sub-ViewModels
    public DashboardViewModel DashboardVM { get; }
    public PatientViewModel PatientVM { get; }
    public DoctorViewModel DoctorVM { get; }
    public VisitViewModel VisitVM { get; }
    public MedicineViewModel MedicineVM { get; }
    public ServiceViewModel ServiceVM { get; }
    public InvoiceViewModel InvoiceVM { get; }

    public Dictionary<string, int> FullPatientNames { get; private set; } = new();
    public Dictionary<string, int> FullDoctorNames { get; private set; } = new();

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

    private string _selectedTheme = "Light";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value)) return;
            ApplyTheme(value, save: true);
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
                OnPropertyChanged(nameof(IsPageServices));
                OnPropertyChanged(nameof(IsPageInvoices));
                OnPropertyChanged(nameof(IsPageSettings));

                _ = value switch
                {
                    "patients"  => PatientVM.LoadAsync(),
                    "doctors"   => DoctorVM.LoadAsync(),
                    "visits"    => VisitVM.LoadAsync(),
                    "medicines" => MedicineVM.LoadAsync(),
                    "services"  => ServiceVM.LoadAsync(),
                    "invoices"  => InvoiceVM.LoadAsync(),
                    "dashboard" => DashboardVM.LoadAsync(),
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
    public bool IsPageServices  => CurrentPage == "services";
    public bool IsPageInvoices  => CurrentPage == "invoices";
    public bool IsPageSettings  => CurrentPage == "settings";

    public RelayCommand NavigateCommand { get; }

    public MainViewModel(
        DatabaseService db, 
        CoreDatabaseService coreDb,
        ToastService toast, 
        ModalService modal,
        DashboardViewModel dashboardVM,
        PatientViewModel patientVM,
        DoctorViewModel doctorVM,
        VisitViewModel visitVM,
        MedicineViewModel medicineVM,
        ServiceViewModel serviceVM,
        InvoiceViewModel invoiceVM)
    {
        Db = db;
        _coreDb = coreDb;
        Toast = toast;
        Modal = modal;
        DashboardVM = dashboardVM;
        PatientVM = patientVM;
        DoctorVM = doctorVM;
        VisitVM = visitVM;
        MedicineVM = medicineVM;
        ServiceVM = serviceVM;
        InvoiceVM = invoiceVM;

        PatientVM.DataChanged += async () => { try { await LoadLookupsAsync(); await DashboardVM.LoadAsync(); } catch { } };
        DoctorVM.DataChanged  += async () => { try { await LoadLookupsAsync(); await DashboardVM.LoadAsync(); } catch { } };
        VisitVM.DataChanged   += async () => { try { await DashboardVM.LoadAsync(); } catch { } };
        ServiceVM.DataChanged += async () => { try { await DashboardVM.LoadAsync(); } catch { } };

        NavigateCommand = new RelayCommand(p => CurrentPage = p?.ToString() ?? "dashboard");
        
        ApplyFontScale("Medium", save: false);
        _ = InitializeAndLoad();
    }

    private async Task InitializeAndLoad()
    {
        try 
        {
            await Task.Delay(50);
            await Task.Run(() => CoreDatabaseService.Initialize());
            await LoadSettingsAsync();
            await LoadLookupsAsync();
            await DashboardVM.LoadAsync();
        } catch { }
    }

    public async Task LoadLookupsAsync()
    {
        try
        {
            FullPatientNames = await Db.GetPatientsLookupAsync();
            FullDoctorNames = await Db.GetDoctorsLookupAsync();
            OnPropertyChanged(nameof(FullPatientNames));
            OnPropertyChanged(nameof(FullDoctorNames));
        }
        catch { }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var level = await _coreDb.GetSettingAsync("FontSizeLevel");
            if (!string.IsNullOrWhiteSpace(level)) { _fontSizeLevel = level; ApplyFontScale(level, save: false); }
            
            var theme = await _coreDb.GetSettingAsync("AppTheme");
            if (!string.IsNullOrWhiteSpace(theme)) { _selectedTheme = theme; ApplyTheme(theme, save: false); }
            else ApplyTheme("Light", save: false);
        } catch { }
    }

    private void ApplyFontScale(string level, bool save)
    {
        var scale = level switch { "Small" => 0.85, "Large" => 1.20, _ => 1.00 };
        FontScale = scale;
        FontScaleManager.UpdateApplicationScale(scale);
        if (save) _ = _coreDb.SetSettingAsync("FontSizeLevel", level);
    }

    private void ApplyTheme(string theme, bool save)
    {
        ThemeManager.ApplyTheme(theme == "Dark" ? ThemeManager.AppTheme.Dark : ThemeManager.AppTheme.Light);
        if (save) _ = _coreDb.SetSettingAsync("AppTheme", theme);
    }
}
