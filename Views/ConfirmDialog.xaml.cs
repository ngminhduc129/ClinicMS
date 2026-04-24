using System.Windows;
using System.Windows.Controls;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class ConfirmDialog : UserControl
{
    private readonly MainViewModel _vm;
    
    public static readonly DependencyProperty IsDeleteActionProperty =
        DependencyProperty.Register(nameof(IsDeleteAction), typeof(bool), typeof(ConfirmDialog), new PropertyMetadata(false));

    public bool IsDeleteAction
    {
        get => (bool)GetValue(IsDeleteActionProperty);
        set => SetValue(IsDeleteActionProperty, value);
    }

    // Đã sửa: Title trước, Message sau
    public ConfirmDialog(string title, string message, MainViewModel vm, bool isDelete = false)
    {
        InitializeComponent();
        _vm = vm;
        TitleTb.Text = title;
        MessageTb.Text = message;
        IsDeleteAction = isDelete;
    }

    private void Yes_Click(object sender, RoutedEventArgs e) => _vm.Modal.Close(true);
    private void No_Click(object sender, RoutedEventArgs e) => _vm.Modal.Close(false);
}
