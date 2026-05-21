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

public partial class MedicineListView : UserControl, INotifyPropertyChanged
{
    private MainViewModel? _vm;
    public ObservableCollection<Medicine> FilteredMedicines { get; } = new();

    public int FilteredCount => FilteredMedicines.Count;

    private enum StockFilter { All, Low, Out }
    private StockFilter _currentStockFilter = StockFilter.All;

    private static readonly SolidColorBrush ActiveBg = new(Color.FromRgb(220, 252, 231));
    private static readonly SolidColorBrush ActiveBorder = new(Color.FromRgb(22, 163, 74));
    private static readonly SolidColorBrush DefaultBg = Brushes.White;
    private static readonly SolidColorBrush DefaultBorder = new(Color.FromRgb(209, 250, 229));

    public MedicineListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        Unloaded += (s, e) => {
            if (_vm?.MedicineVM?.Medicines != null)
                _vm.MedicineVM.Medicines.CollectionChanged -= OnMedicinesChanged;
        };
    }

    private async void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.MedicineVM.Medicines.CollectionChanged -= OnMedicinesChanged;

        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            _vm.MedicineVM.Medicines.CollectionChanged += OnMedicinesChanged;
            try
            {
                await _vm.MedicineVM.LoadAsync();
                UpdateFilterButtonStates();
                ApplyFilter();
                UpdateStats();
            }
            catch (Exception ex) { _vm.Toast.ShowError($"Lỗi: {ex.Message}"); }
        }
    }

    private void OnMedicinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilter();

    private string _search = "";
    private void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        if (s is TextBox tb) { _search = tb.Text.Trim().ToLower(); ApplyFilter(); }
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

        var query = _vm.MedicineVM.Medicines.Where(m =>
            (string.IsNullOrEmpty(search) ||
             (m.MedicineName != null && m.MedicineName.ToLower().Contains(search)) ||
             (m.Unit != null && m.Unit.ToLower().Contains(search)))
            && (_currentStockFilter switch {
                StockFilter.Low => m.AvailableQuantity > 0 && m.AvailableQuantity <= 10,
                StockFilter.Out => m.AvailableQuantity <= 0,
                _ => true
            })
        ).ToList();

        FilteredMedicines.Clear();
        foreach (var m in query) FilteredMedicines.Add(m);

        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(FilteredMedicines));
        UpdateStats();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void UpdateStats()
    {
        if (_vm == null) return;
        double totalValue = _vm.MedicineVM.Medicines.Sum(m => m.StockValue);
        int lowStockCount = _vm.MedicineVM.Medicines.Count(m => m.AvailableQuantity > 0 && m.AvailableQuantity <= 10);

        if (TxtTotalValue != null) TxtTotalValue.Text = totalValue.ToString("N0") + " ₫";
        if (TxtLowStockCount != null) TxtLowStockCount.Text = lowStockCount.ToString();
    }

    private void FilterAll_Click(object s, RoutedEventArgs e)
    {
        _currentStockFilter = StockFilter.All;
        UpdateFilterButtonStates();
        ApplyFilter();
    }

    private void FilterLowStock_Click(object s, RoutedEventArgs e)
    {
        _currentStockFilter = StockFilter.Low;
        UpdateFilterButtonStates();
        ApplyFilter();
    }

    private void FilterOutOfStock_Click(object s, RoutedEventArgs e)
    {
        _currentStockFilter = StockFilter.Out;
        UpdateFilterButtonStates();
        ApplyFilter();
    }

    private void UpdateFilterButtonStates()
    {
        if (FilterAllBtn == null || FilterLowBtn == null || FilterOutBtn == null) return;

        var (activeBg, activeBd, defaultBg, defaultBd) = (ActiveBg, ActiveBorder, DefaultBg, DefaultBorder);

        // Reset all
        FilterAllBtn.Background = defaultBg;
        FilterAllBtn.BorderBrush = defaultBd;
        FilterLowBtn.Background = defaultBg;
        FilterLowBtn.BorderBrush = defaultBd;
        FilterOutBtn.Background = defaultBg;
        FilterOutBtn.BorderBrush = defaultBd;

        // Highlight active
        var target = _currentStockFilter switch
        {
            StockFilter.All => FilterAllBtn,
            StockFilter.Low => FilterLowBtn,
            StockFilter.Out => FilterOutBtn,
            _ => null
        };

        if (target != null)
        {
            target.Background = activeBg;
            target.BorderBrush = activeBd;
        }
    }

    private async void Refresh_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        try { await _vm.MedicineVM.LoadAsync(); _vm.Toast.Show("Đã làm mới dữ liệu"); }
        catch (Exception ex) { _vm.Toast.ShowError(ex.Message); }
    }

    private void Add_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var form = new MedicineFormDialog(_vm);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.MedicineVM.LoadAsync(); }, () => !form.HasData());
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s is not FrameworkElement el) || el.DataContext is not Medicine m) return;
        var form = new MedicineFormDialog(_vm, m);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.MedicineVM.LoadAsync(); }, () => !form.HasData());
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || (s is not FrameworkElement el) || el.DataContext is not Medicine m) return;
        var confirm = new ConfirmDialog($"Xóa thuốc \"{m.MedicineName}\"?", "Thao tác không thể hoàn tác.", _vm);
        _vm.Modal.Show(confirm, async result => {
            if (result == true) { var ok = await _vm.MedicineVM.DeleteAsync(m); if (ok) ApplyFilter(); }
        });
    }
}
