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

    public PatientListView()
    {
        InitializeComponent();
        
        this.DataContextChanged += (s, e) => {
            if (e.NewValue is MainViewModel vm) {
                _vm = vm;
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
                await _vm.PatientVM.LoadMoreAsync();
            }
        }
    }

    private int _searchRequestCount = 0;
    private async void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        if (_vm == null) return;
        
        string search = ((TextBox)s).Text.Trim();
        int currentRequest = ++_searchRequestCount;
        
        await Task.Delay(300);
        if (currentRequest != _searchRequestCount) return;

        await _vm.PatientVM.SearchAsync(search);
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

    private async void FilterGender_Click(object s, RoutedEventArgs e)
    {
        if (s is MenuItem mi && _vm != null)
        {
            await _vm.PatientVM.SearchAsync(mi.Tag?.ToString() ?? "");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void RefreshButton_Click(object s, RoutedEventArgs e)
    {
        if (_vm != null) await _vm.PatientVM.LoadAsync();
    }

    private void AddPatient_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var form = new PatientFormDialog(_vm);
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.PatientVM.LoadAsync(); }, () => form.HasData());
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
        _vm.Modal.Show(form, result => { if (result == true) _ = _vm.PatientVM.LoadAsync(); }, () => form.HasData());
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || ((Button)s).DataContext is not Patient p) return;
        var confirm = new ConfirmDialog($"Xóa bệnh nhân \"{p.FullName}\"?",
            "Thao tác này sẽ xóa tất cả hồ sơ khám liên quan và không thể hoàn tác.", _vm);
        _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.PatientVM.DeleteAsync(p); });
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
}
