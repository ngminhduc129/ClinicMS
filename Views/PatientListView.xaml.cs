using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class PatientListView : UserControl, INotifyPropertyChanged
{
    private MainViewModel? _vm;
    public ObservableCollection<Patient> FilteredPatients { get; } = new();

    public int FilteredCount 
    {
        get 
        {
            if (_vm != null && string.IsNullOrEmpty(_search) && string.IsNullOrEmpty(_genderFilter))
                return _vm.TotalPatients;
            return FilteredPatients.Count;
        }
    }

    public PatientListView()
    {
        InitializeComponent();
        
        this.DataContextChanged += (s, e) => {
            if (e.NewValue is MainViewModel vm) {
                _vm = vm;
                _vm.Patients.CollectionChanged += OnPatientsCollectionChanged;
                _vm.PropertyChanged += (s2, e2) => {
                    if (e2.PropertyName == nameof(MainViewModel.TotalPatients))
                        OnPropertyChanged(nameof(FilteredCount));
                };
                ApplyFilter();
            }
        };

        PatientDataGrid.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));

        TableCard.SizeChanged += (s, e) =>
        {
            TableCard.Clip = new RectangleGeometry(
                new Rect(0, 0, TableCard.ActualWidth, TableCard.ActualHeight),
                18, 18);
        };
    }

    private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
        {
            if (_vm != null)
            {
                await _vm.LoadMorePatientsAsync();
            }
        }
    }

    private bool _isUpdatingCollection = false;
    private void OnPatientsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdatingCollection) return;

        Dispatcher.BeginInvoke(() => {
            _isUpdatingCollection = true;
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    var gender = _genderFilter;
                    foreach (Patient p in e.NewItems)
                    {
                        if (!FilteredPatients.Any(x => x.PatientId == p.PatientId))
                        {
                            if (string.IsNullOrEmpty(gender) || p.Gender == gender)
                                FilteredPatients.Add(p);
                        }
                    }
                    OnPropertyChanged(nameof(FilteredCount));
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    FilteredPatients.Clear();
                    ApplyFilter();
                }
                else
                {
                    ApplyFilter();
                }
            }
            finally
            {
                _isUpdatingCollection = false;
            }
        });
    }

    private string _search = "";
    private string _genderFilter = "";

    private void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        _search = ((TextBox)s).Text.Trim().ToLower();
        ApplyFilter();
    }

    private void FilterButton_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void FilterGender_Click(object s, RoutedEventArgs e)
    {
        if (s is MenuItem mi)
        {
            _genderFilter = mi.Tag?.ToString() ?? "";
            ApplyFilter();
        }
    }

    private int _filterRequestCount = 0;
    private async void ApplyFilter()
    {
        if (_vm == null || _isUpdatingCollection) return;
        
        int currentRequest = ++_filterRequestCount;
        
        if (!string.IsNullOrEmpty(_search)) {
            await Task.Delay(300);
            if (currentRequest != _filterRequestCount) return;
        }

        var search = _search?.Trim() ?? "";
        
        if (!string.IsNullOrEmpty(search))
        {
            await _vm.SearchPatientsAsync(search);
        }

        var gender = _genderFilter;
        var sourceItems = _vm.Patients.Where(p => 
            (string.IsNullOrEmpty(gender) || p.Gender == gender)
        ).ToList();

        if (currentRequest == _filterRequestCount)
        {
            SyncCollection(sourceItems);
        }
    }

    private void SyncCollection(List<Patient> newItems)
    {
        _isUpdatingCollection = true;
        try
        {
            // Kiểm tra xem có thực sự cần cập nhật không
            if (FilteredPatients.Count == newItems.Count)
            {
                bool isSame = true;
                for (int i = 0; i < newItems.Count; i++) {
                    if (FilteredPatients[i].PatientId != newItems[i].PatientId) { isSame = false; break; }
                }
                if (isSame) return;
            }

            // 1. Loại bỏ mục cũ (Surgical Remove) - Không làm Reset thanh cuộn
            var newIds = new HashSet<int>(newItems.Select(x => x.PatientId));
            for (int i = FilteredPatients.Count - 1; i >= 0; i--)
            {
                if (!newIds.Contains(FilteredPatients[i].PatientId))
                    FilteredPatients.RemoveAt(i);
            }

            // 2. Thêm mục mới (Surgical Add)
            var existingIds = new HashSet<int>(FilteredPatients.Select(x => x.PatientId));
            foreach (var item in newItems)
            {
                if (!existingIds.Contains(item.PatientId))
                    FilteredPatients.Add(item);
            }

            OnPropertyChanged(nameof(FilteredCount));
        }
        finally
        {
            _isUpdatingCollection = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void RefreshButton_Click(object s, RoutedEventArgs e)
    {
        if (_vm != null) await _vm.LoadPatientsAsync();
    }

    private void AddPatient_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var form = new PatientFormDialog(_vm);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadPatientsAsync(); }, () => form.HasData());
    }

    private void Detail_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || ((Button)s).DataContext is not Patient p) return;
        var detail = new PatientDetailDialog(_vm, p);
        _vm.Modal.Show(detail);
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || ((Button)s).DataContext is not Patient p) return;
        var form = new PatientFormDialog(_vm, p);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadPatientsAsync(); }, () => form.HasData());
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || ((Button)s).DataContext is not Patient p) return;
        var confirm = new ConfirmDialog($"Xóa bệnh nhân \"{p.FullName}\"?",
            "Thao tác này sẽ xóa tất cả hồ sơ khám liên quan và không thể hoàn tác.", _vm);
        _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.DeletePatientAsync(p); });
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
}
