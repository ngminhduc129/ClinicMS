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
                ApplyFilter();
            }
        };
    }

    private string _search = "";
    private string _specialtyFilter = "";
    private System.Threading.CancellationTokenSource? _searchCts;

    private async void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        _search = ((TextBox)s).Text.Trim();
        
        // Debounce: Hủy tác vụ cũ nếu người dùng vẫn đang gõ
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;

        try 
        {
            await Task.Delay(300, token); // Chờ 300ms
            ApplyFilter();
        }
        catch (TaskCanceledException) { /* Bỏ qua nếu bị hủy */ }
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

    private async void ApplyFilter()
    {
        if (_vm == null) return;

        var search = _search?.Trim() ?? "";

        // Nếu có từ khóa, gọi hàm search từ Database
        if (!string.IsNullOrEmpty(search))
        {
            await _vm.DoctorVM.SearchAsync(search);
        }
        else
        {
            // Nếu không tìm kiếm, nạp lại toàn bộ bác sĩ
            await _vm.DoctorVM.LoadAsync();
        }

        var q = _vm.DoctorVM.Doctors.Where(d => 
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

    private async void RefreshButton_Click(object s, RoutedEventArgs e) { if (_vm != null) await _vm.DoctorVM.LoadAsync(); }

    private void AddDoctor_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var form = new DoctorFormDialog(_vm);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.DoctorVM.LoadAsync(); }, () => !form.HasData());
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
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.DoctorVM.LoadAsync(); }, () => !form.HasData());
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s as Button)?.DataContext is not Doctor d) return;
        var confirm = new ConfirmDialog($"Xóa bác sĩ \"{d.FullName}\"?", "Thao tác không thể hoàn tác.", _vm);
        _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.DoctorVM.DeleteAsync(d); });
    }

    private void DoctorGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
}
