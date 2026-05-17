using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

public partial class ServiceListView : UserControl, INotifyPropertyChanged
{
    private MainViewModel? _vm;
    
    // We'll use the ViewModel's collection directly, but keep this for consistency if needed.
    // However, ServiceViewModel already has filtering logic in SearchAsync.
    // For simplicity and matching existing patterns, let's use a local filtered collection.
    public ObservableCollection<Service> FilteredServices { get; } = new();

    public ServiceListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        
        Unloaded += (s, e) => {
            if (_vm?.ServiceVM?.Services != null)
                _vm.ServiceVM.Services.CollectionChanged -= OnServicesChanged;
        };
    }

    private async void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.ServiceVM.Services.CollectionChanged -= OnServicesChanged;

        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            _vm.ServiceVM.Services.CollectionChanged += OnServicesChanged;
            try 
            { 
                await _vm.ServiceVM.LoadAsync(); 
                ApplyFilter(); 
            }
            catch (Exception ex) { _vm.Toast.ShowError($"Lỗi: {ex.Message}"); }
        }
    }

    private void OnServicesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilter();

    private string _search = "";
    private void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        if (s is TextBox tb) 
        { 
            _search = tb.Text.Trim().ToLower(); 
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

        var query = _vm.ServiceVM.Services.Where(s =>
            (string.IsNullOrEmpty(search) || 
             (s.ServiceName != null && s.ServiceName.ToLower().Contains(search)))
        ).ToList();

        FilteredServices.Clear();
        foreach (var s in query) FilteredServices.Add(s);

        OnPropertyChanged(nameof(FilteredServices));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void Refresh_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        try { await _vm.ServiceVM.LoadAsync(); _vm.Toast.Show("Đã làm mới dữ liệu"); }
        catch (Exception ex) { _vm.Toast.ShowError(ex.Message); }
    }

    private void TableCard_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Border b)
        {
            b.Clip = new RectangleGeometry(new Rect(0, 0, b.ActualWidth, b.ActualHeight), 10, 10);
        }
    }

    private void Add_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var form = new ServiceFormDialog(_vm);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.ServiceVM.LoadAsync(); });
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s is not FrameworkElement el) || el.DataContext is not Service svc) return;
        var form = new ServiceFormDialog(_vm, svc);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.ServiceVM.LoadAsync(); });
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s is not FrameworkElement el) || el.DataContext is not Service svc) return;
        var confirm = new ConfirmDialog($"Xóa dịch vụ \"{svc.ServiceName}\"?", "Thao tác không thể hoàn tác.", _vm);
        _vm.Modal.Show(confirm, async result => { 
            if (result == true) { var ok = await _vm.ServiceVM.DeleteAsync(svc); if (ok) ApplyFilter(); }
        });
    }
}
