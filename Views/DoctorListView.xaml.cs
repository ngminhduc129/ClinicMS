using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class DoctorListView : UserControl, INotifyPropertyChanged
{
    private MainViewModel? _vm;
    public ObservableCollection<Doctor> FilteredDoctors { get; } = new();

    public int FilteredCount => FilteredDoctors.Count;

    public DoctorListView()
    {
        InitializeComponent();
        
        this.DataContextChanged += (s, e) => {
            if (e.NewValue is MainViewModel vm) {
                _vm = vm;
                _vm.Doctors.CollectionChanged += (s2, e2) => ApplyFilter();
                ApplyFilter();
            }
        };

        TableCard.SizeChanged += (s, e) =>
        {
            TableCard.Clip = new System.Windows.Media.RectangleGeometry(
                new Rect(0, 0, TableCard.ActualWidth, TableCard.ActualHeight),
                18, 18);
        };
    }

    private string _search = "";
    private string _specialtyFilter = "";

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

    private void FilterSpecialty_Click(object s, RoutedEventArgs e)
    {
        if (s is MenuItem mi)
        {
            _specialtyFilter = mi.Tag?.ToString() ?? "";
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        if (_vm == null) return;

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ApplyFilter);
            return;
        }

        var search = _search?.Trim().ToLower() ?? "";
        
        var q = _vm.Doctors.Where(d =>
            (string.IsNullOrEmpty(search) ||
            (d.FullName != null && d.FullName.ToLower().Contains(search)) ||
            (d.Specialty != null && d.Specialty.ToLower().Contains(search)) ||
            (d.DoctorId.ToString().Contains(search))) &&
            (string.IsNullOrEmpty(_specialtyFilter) || d.Specialty == _specialtyFilter)
        ).ToList();
        
        FilteredDoctors.Clear();
        foreach (var d in q) FilteredDoctors.Add(d);
        
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(FilteredDoctors));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void RefreshButton_Click(object s, RoutedEventArgs e) { if (_vm != null) await _vm.LoadDoctorsAsync(); }

    private void AddDoctor_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var form = new DoctorFormDialog(_vm);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadDoctorsAsync(); }, () => form.HasData());
    }

    private void Detail_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s as Button)?.DataContext is not Doctor d) return;
        var detail = new DoctorDetailDialog(_vm, d);
        _vm.Modal.Show(detail);
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s as Button)?.DataContext is not Doctor d) return;
        var form = new DoctorFormDialog(_vm, d);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadDoctorsAsync(); }, () => form.HasData());
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s as Button)?.DataContext is not Doctor d) return;
        var confirm = new ConfirmDialog($"Xóa bác sĩ \"{d.FullName}\"?", "Thao tác không thể hoàn tác.", _vm);
        _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.DeleteDoctorAsync(d); });
    }

    private void DoctorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
}
