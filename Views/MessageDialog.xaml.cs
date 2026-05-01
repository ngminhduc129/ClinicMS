using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class MessageDialog : UserControl
{
    private readonly MainViewModel _vm;

    public MessageDialog(MainViewModel vm, string message, string title = "Thông báo", bool isError = false)
    {
        InitializeComponent();
        _vm = vm;
        Opacity = 0;
        
        TitleTxt.Text = title;
        MessageTxt.Text = message;
        
        if (isError)
        {
            IconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
            IconTxt.Text = "\uE783"; // Warning icon
            IconTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            OkBtn.Tag = "Error"; // Can use to change button color if needed
        }
        else
        {
            IconBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            IconTxt.Text = "\uE73E"; // Check icon
            IconTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
        }

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

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _vm.Modal.Close();
    }
}
