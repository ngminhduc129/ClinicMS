using System;
using System.Windows;
using System.Windows.Controls;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class DashboardView : UserControl
{
    private MainViewModel? _vm;

    public DashboardView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_vm != null) await _vm.DashboardVM.LoadAsync();
    }
}
