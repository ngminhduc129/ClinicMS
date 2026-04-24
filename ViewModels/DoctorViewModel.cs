using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class DoctorViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    public ObservableCollection<Doctor> Doctors { get; } = new();

    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    public DoctorViewModel(DatabaseService db)
    {
        _db = db;
    }

    public async Task LoadAsync()
    {
        Loading = true;
        try 
        { 
            var list = await _db.GetDoctorsAsync(); 
            Doctors.Clear(); 
            foreach (var d in list) Doctors.Add(d); 
        }
        catch { } 
        finally { Loading = false; }
    }

    public async Task SearchAsync(string term)
    {
        Loading = true;
        try {
            var results = await _db.SearchDoctorsAsync(term);
            Doctors.Clear();
            foreach (var d in results) Doctors.Add(d);
        } finally { Loading = false; }
    }

    public async Task<bool> AddAsync(Doctor d) { try { await _db.AddDoctorAsync(d); await LoadAsync(); return true; } catch { return false; } }
    public async Task<bool> UpdateAsync(Doctor d) { try { await _db.UpdateDoctorAsync(d); await LoadAsync(); return true; } catch { return false; } }
    public async Task<bool> DeleteAsync(Doctor d) { try { await _db.DeleteDoctorAsync(d.DoctorId); await LoadAsync(); return true; } catch { return false; } }
}
