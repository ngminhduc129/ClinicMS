using System;
using System.Windows;
using System.Windows.Input;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        vm.Modal.Register(ModalOverlay, ModalHost);
    }

    private void ModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Modal.RequestClose();
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        // Xóa logic scale cứng tại đây để tránh xung đột với FontScale điều chỉnh bởi người dùng
    }
}
