using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class PatientViewModel : BaseViewModel
{
    private readonly DatabaseService _db;
    private readonly ToastService _toast;

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

    public PatientViewModel(DatabaseService db, ToastService toast)
    {
        _db = db;
        _toast = toast;
    }

    public async Task LoadAsync()
    {
        if (Loading) return;
        Loading = true;
        try 
        {
            if (_offset == 0)
            {
                Patients.Clear();
                _totalCount = await _db.GetPatientsSearchCountAsync(SearchTerm);
                OnPropertyChanged(nameof(TotalPatientsCount));
            }

            var results = await _db.SearchPatientsAsync(SearchTerm, _offset, PageSize);
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

    public async Task SearchAsync(string term)
    {
        SearchTerm = term;
        _offset = 0;
        await LoadAsync();
    }

    public async Task LoadMoreAsync()
    {
        if (Loading || !HasMore) return;
        await LoadAsync();
    }

    public event Action? DataChanged;

    public async Task<bool> AddAsync(Patient p)
    {
        try { await _db.AddPatientAsync(p); _offset = 0; await LoadAsync(); DataChanged?.Invoke(); return true; } catch { return false; }
    }

    public async Task<bool> UpdateAsync(Patient p)
    {
        try { await _db.UpdatePatientAsync(p); _offset = 0; await LoadAsync(); DataChanged?.Invoke(); return true; } catch { return false; }
    }

    public async Task<bool> DeleteAsync(Patient p)
    {
        try { await _db.DeletePatientAsync(p.PatientId); _offset = 0; await LoadAsync(); DataChanged?.Invoke(); return true; } catch { return false; }
    }

    public async Task<(Patient? p, Contact? contact)> GetPatientWithContactAsync(int id) => await _db.GetPatientWithContactAsync(id);
    public async Task<List<Visit>> GetPatientVisitsAsync(int id) => await _db.GetPatientVisitsAsync(id);
}
