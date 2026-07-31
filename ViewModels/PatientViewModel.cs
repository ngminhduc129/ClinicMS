using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class PatientViewModel : BaseViewModel
{
    private readonly PatientService _patientService;
    private readonly ToastService _toast;
    private readonly SignalRClientService _signalR;

    public ObservableCollection<Patient> Patients { get; } = new();
    
    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    private int _offset = 0;
    private int _totalCount;
    private bool _hasMore;
    public bool HasMore { get => _hasMore; set => SetProperty(ref _hasMore, value); }

    public int TotalPatientsCount => _totalCount;

    private string _searchTerm = "";
    public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }

    private const int PageSize = 50;

    public PatientViewModel(PatientService patientService, ToastService toast, SignalRClientService signalR)
    {
        _patientService = patientService;
        _toast = toast;
        _signalR = signalR;
    }

    public async Task LoadAsync()
    {
        if (Loading) return;
        Loading = true;
        try 
        {
            _offset = 0;
            Patients.Clear();
            _totalCount = await _patientService.GetPatientsSearchCountAsync(SearchTerm);
            OnPropertyChanged(nameof(TotalPatientsCount));

            var results = await _patientService.SearchPatientsAsync(SearchTerm, _offset, PageSize);
            foreach (var p in results) Patients.Add(p);
            
            _offset += results.Count;
            HasMore = Patients.Count < _totalCount;
        } 
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải danh sách bệnh nhân: {ex.Message}");
        } 
        finally { Loading = false; }
    }

    public async Task LoadMoreAsync()
    {
        if (Loading || !HasMore) return;
        Loading = true;
        try
        {
            var results = await _patientService.SearchPatientsAsync(SearchTerm, _offset, PageSize);
            foreach (var p in results) Patients.Add(p);
            _offset += results.Count;
            HasMore = Patients.Count < _totalCount;
        }
        catch { }
        finally { Loading = false; }
    }

    public async Task SearchAsync(string term)
    {
        SearchTerm = term;
        _offset = 0;
        await LoadAsync();
    }



    public event Action? DataChanged;

    public async Task<bool> AddAsync(Patient p)
    {
        try { await _patientService.AddPatientAsync(p); _offset = 0; await LoadAsync(); DataChanged?.Invoke(); await _signalR.NotifyDataChangedAsync("patients"); return true; } catch { return false; }
    }

    public async Task<bool> UpdateAsync(Patient p)
    {
        try { await _patientService.UpdatePatientAsync(p); _offset = 0; await LoadAsync(); DataChanged?.Invoke(); await _signalR.NotifyDataChangedAsync("patients"); return true; } catch { return false; }
    }

    public async Task<bool> DeleteAsync(Patient p)
    {
        try { await _patientService.DeletePatientAsync(p.PatientId); _offset = 0; await LoadAsync(); DataChanged?.Invoke(); await _signalR.NotifyDataChangedAsync("patients"); return true; } catch { return false; }
    }

    public async Task<(Patient? p, Contact? contact)> GetPatientWithContactAsync(int id) => await _patientService.GetPatientWithContactAsync(id);

    public async Task<List<Patient>> QuickSearchAsync(string term, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(term)) return new();
        return await _patientService.SearchPatientsAsync(term, 0, limit);
    }
    
    // This will be updated later when VisitService is ready, for now it's still in DatabaseService
    public async Task<List<Visit>> GetPatientVisitsAsync(int id) 
    {
        // For temporary compatibility, we use the old Db from MainViewModel if needed, 
        // but let's just move it to PatientService to be clean.
        // I will add GetPatientVisitsAsync to PatientService in the next step.
        // Wait, I already added it to DatabaseService, I should move it to PatientService.
        return await _patientService.GetPatientVisitsAsync(id);
    }
}
