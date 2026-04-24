using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClinicMS.Models;
using ClinicMS.Services;

namespace ClinicMS.ViewModels;

public class MedicineViewModel : BaseViewModel
{
    private readonly DatabaseService _db;

    public ObservableCollection<Medicine> Medicines { get; } = new();

    private bool _loading;
    public bool Loading { get => _loading; set => SetProperty(ref _loading, value); }

    public MedicineViewModel(DatabaseService db)
    {
        _db = db;
    }

    public async Task LoadAsync()
    {
        Loading = true;
        try { var list = await _db.GetMedicinesAsync(); Medicines.Clear(); foreach (var m in list) Medicines.Add(m); }
        catch { } finally { Loading = false; }
    }

    public async Task<bool> AddAsync(Medicine m) { try { await _db.AddMedicineAsync(m); await LoadAsync(); return true; } catch { return false; } }
    public async Task<bool> UpdateAsync(Medicine m) { try { await _db.UpdateMedicineAsync(m); await LoadAsync(); return true; } catch { return false; } }
    public async Task<bool> DeleteAsync(Medicine m) { try { await _db.DeleteMedicineAsync(m.MedicineId); await LoadAsync(); return true; } catch { return false; } }
}
