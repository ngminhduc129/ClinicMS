using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class MedicineViewModel : BaseViewModel
{
    private readonly MedicineService _medicineService;
    private readonly ToastService _toast;

    public ObservableCollection<Medicine> Medicines { get; } = new();

    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    public MedicineViewModel(MedicineService medicineService, ToastService toast)
    {
        _medicineService = medicineService;
        _toast = toast;
    }

    public async Task LoadAsync()
    {
        Loading = true;
        try { 
            var list = await _medicineService.GetMedicinesAsync(); 
            Medicines.Clear(); 
            foreach (var m in list) Medicines.Add(m); 
        }
        catch (Exception ex)
        {
            _toast.ShowError($"Lỗi khi tải danh sách thuốc: {ex.Message}");
        } 
        finally { Loading = false; }
    }

    public async Task<bool> AddAsync(Medicine m) { try { await _medicineService.AddMedicineAsync(m); await LoadAsync(); return true; } catch { return false; } }
    public async Task<bool> UpdateAsync(Medicine m) { try { await _medicineService.UpdateMedicineAsync(m); await LoadAsync(); return true; } catch { return false; } }
    public async Task<bool> DeleteAsync(Medicine m) { try { await _medicineService.DeleteMedicineAsync(m.MedicineId); await LoadAsync(); return true; } catch { return false; } }
}
