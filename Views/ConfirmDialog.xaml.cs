using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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
        Opacity = 0;
        TitleTb.Text = title;
        MessageTb.Text = message;
        IsDeleteAction = isDelete;

        Loaded += (_, _) => AnimateOpen();
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.4);

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        Timeline.SetDesiredFrameRate(animOpacity, 60);
        animOpacity.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
    }

    private void Yes_Click(object sender, RoutedEventArgs e) => _vm.Modal.Close(true);
    private void No_Click(object sender, RoutedEventArgs e) => _vm.Modal.Close(false);
}
