using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class DoctorViewModel : BaseViewModel
{
    private readonly DoctorService _doctorService;
    private readonly ToastService _toast;
    private readonly SignalRClientService _signalR;

    public ObservableCollection<Doctor> Doctors { get; } = new();

    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    public DoctorViewModel(DoctorService doctorService, ToastService toast, SignalRClientService signalR)
    {
        _doctorService = doctorService;
        _toast = toast;
        _signalR = signalR;
    }

    public async Task LoadAsync()
    {
        Loading = true;
        try 
        { 
            var list = await _doctorService.GetDoctorsAsync(); 
            Doctors.Clear(); 
            foreach (var d in list) Doctors.Add(d); 
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải danh sách bác sĩ: {ex.Message}");
        } 
        finally { Loading = false; }
    }

    public async Task SearchAsync(string term)
    {
        Loading = true;
        try {
            var results = await _doctorService.SearchDoctorsAsync(term);
            Doctors.Clear();
            foreach (var d in results) Doctors.Add(d);
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tìm kiếm bác sĩ: {ex.Message}");
        }
        finally { Loading = false; }
    }

    public event Action? DataChanged;

    public async Task<bool> AddAsync(Doctor d) { try { await _doctorService.AddDoctorAsync(d); await LoadAsync(); DataChanged?.Invoke(); await _signalR.NotifyDataChangedAsync("doctors"); return true; } catch { return false; } }
    public async Task<bool> UpdateAsync(Doctor d) { try { await _doctorService.UpdateDoctorAsync(d); await LoadAsync(); DataChanged?.Invoke(); await _signalR.NotifyDataChangedAsync("doctors"); return true; } catch { return false; } }
    public async Task<bool> DeleteAsync(Doctor d) { try { await _doctorService.DeleteDoctorAsync(d.DoctorId); await LoadAsync(); DataChanged?.Invoke(); await _signalR.NotifyDataChangedAsync("doctors"); return true; } catch { return false; } }
}
