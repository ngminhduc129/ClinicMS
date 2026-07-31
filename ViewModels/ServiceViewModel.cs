using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class ServiceViewModel : BaseViewModel
{
    private readonly ServiceCatalogService _serviceService;
    private readonly ToastService _toast;
    private readonly SignalRClientService _signalR;

    public ObservableCollection<Service> Services { get; } = new();
    
    public event Action? DataChanged;

    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    public ServiceViewModel(ServiceCatalogService serviceService, ToastService toast, SignalRClientService signalR)
    {
        _serviceService = serviceService;
        _toast = toast;
        _signalR = signalR;
    }

    public async Task LoadAsync()
    {
        Loading = true;
        try 
        { 
            var list = await _serviceService.GetServicesAsync(); 
            Services.Clear(); 
            foreach (var s in list) Services.Add(s); 
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải danh sách dịch vụ: {ex.Message}");
        } 
        finally { Loading = false; }
    }

    public async Task SearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            await LoadAsync();
            return;
        }

        try
        {
            var list = await _serviceService.GetServicesAsync();
            var filtered = list.Where(s => s.ServiceName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            Services.Clear();
            foreach (var s in filtered) Services.Add(s);
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tìm kiếm dịch vụ: {ex.Message}");
        }
    }

    public async Task<bool> AddAsync(Service s) 
    { 
        try 
        { 
            await _serviceService.AddServiceAsync(s); 
            await LoadAsync(); 
            DataChanged?.Invoke();
            await _signalR.NotifyDataChangedAsync("services");
            return true; 
        } 
        catch (Exception ex) 
        { 
            _toast.ShowError($"Lỗi khi thêm dịch vụ: {ex.Message}");
            return false; 
        } 
    }

    public async Task<bool> UpdateAsync(Service s) 
    { 
        try 
        { 
            await _serviceService.UpdateServiceAsync(s); 
            await LoadAsync(); 
            DataChanged?.Invoke();
            await _signalR.NotifyDataChangedAsync("services");
            return true; 
        } 
        catch (Exception ex) 
        { 
            _toast.ShowError($"Lỗi khi cập nhật dịch vụ: {ex.Message}");
            return false; 
        } 
    }

    public async Task<bool> DeleteAsync(Service s) 
    { 
        try 
        { 
            await _serviceService.DeleteServiceAsync(s.ServiceId); 
            await LoadAsync(); 
            DataChanged?.Invoke();
            await _signalR.NotifyDataChangedAsync("services");
            return true; 
        } 
        catch (Exception ex) 
        { 
            _toast.ShowError($"Lỗi khi xóa dịch vụ: {ex.Message}");
            return false; 
        } 
    }
}
