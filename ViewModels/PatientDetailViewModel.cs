using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ClinicMS.Models;
using ClinicMS.Services;
using ClinicMS.Views;

namespace ClinicMS.ViewModels;

public class PatientDetailViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;
    private Patient _patient;
    private Contact? _contact;
    private bool _isLoading;

    public PatientDetailViewModel(MainViewModel mainVm, Patient patient)
    {
        _mainVm = mainVm;
        _patient = patient;
        Visits = new ObservableCollection<Visit>();
    }

    public Patient Patient
    {
        get => _patient;
        set => SetProperty(ref _patient, value);
    }

    public Contact? Contact
    {
        get => _contact;
        set => SetProperty(ref _contact, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<Visit> Visits { get; }

    public string Initials => Patient?.Initials ?? "";
    public string FullName => Patient?.FullName ?? "N/A";
    public string GenderDob => $"{Patient?.Gender ?? "—"}  •  {Patient?.DateOfBirth ?? "—"}";

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _mainVm.PatientVM.GetPatientWithContactAsync(_patient.PatientId);
            if (result.p != null)
            {
                Patient = result.p;
                Contact = result.contact;
                
                // Cập nhật các thuộc tính phụ thuộc
                OnPropertyChanged(nameof(Initials));
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(GenderDob));
            }

            var visits = await _mainVm.PatientVM.GetPatientVisitsAsync(_patient.PatientId);
            Visits.Clear();
            foreach (var v in visits) Visits.Add(v);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Close()
    {
        _mainVm.Modal.Close();
    }

    public void Edit()
    {
        var form = new Views.PatientFormDialog(_mainVm, Patient);
        _mainVm.Modal.Show(form, async (result) => 
        { 
            if (result == true) await LoadDataAsync(); 
        }, () => form.HasData());
    }
}
