using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
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
        
        // Theo dõi khi DataContext (MainViewModel) được gán vào trang
        this.DataContextChanged += (s, e) => {
            if (e.NewValue is MainViewModel vm) {
                _vm = vm;
                _vm.Patients.CollectionChanged += OnPatientsCollectionChanged;
                // Lắng nghe thay đổi tổng số bệnh nhân từ ViewModel
                _vm.PropertyChanged += (s2, e2) => {
                    if (e2.PropertyName == nameof(MainViewModel.TotalPatients))
                        OnPropertyChanged(nameof(FilteredCount));
                };
                ApplyFilter();
            }
        };

        // Gắn sự kiện cuộn chuột cho DataGrid
        PatientDataGrid.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));

        // Bo góc card đúng cách
        TableCard.SizeChanged += (s, e) =>
        {
            TableCard.Clip = new RectangleGeometry(
                new Rect(0, 0, TableCard.ActualWidth, TableCard.ActualHeight),
                18, 18);
        };
    }

    private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Nếu cuộn xuống gần hết (còn cách đáy 100 pixel)
        if (e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
        {
            if (_vm != null)
            {
                await _vm.LoadMorePatientsAsync();
            }
        }
    }

    private void OnPatientsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        ApplyFilter();
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
        if (_vm == null) return;
        
        int currentRequest = ++_filterRequestCount;
        // Debounce: Chờ một chút nếu có nhiều yêu cầu dồn dập (ví dụ khi nạp 500 bản ghi)
        await Task.Delay(50);
        if (currentRequest != _filterRequestCount) return;

        var search = _search?.Trim().ToLower() ?? "";
        var gender = _genderFilter;
        
        // Tạo bản sao của danh sách trên UI thread để tránh lỗi "Destination array was not long enough"
        var source = _vm.Patients.ToList();

        // Chạy việc lọc ở background thread để không làm khựng UI
        var filteredList = await Task.Run(() => {
            return source.Where(p => 
                (string.IsNullOrEmpty(search) ||
                 (p.FullName != null && p.FullName.ToLower().Contains(search)) ||
                 (p.Phone != null && p.Phone.ToLower().Contains(search)) ||
                 (p.PatientId.ToString().Contains(search))) &&
                (string.IsNullOrEmpty(gender) || p.Gender == gender)
            ).ToList();
        });

        // Cập nhật kết quả lên UI thread
        UpdateFilteredCollection(filteredList);
    }

    private void UpdateFilteredCollection(List<Patient> items)
    {
        FilteredPatients.Clear();
        foreach (var p in items) FilteredPatients.Add(p);
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(FilteredPatients));
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
