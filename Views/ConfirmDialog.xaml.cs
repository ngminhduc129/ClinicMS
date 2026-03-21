using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class ConfirmDialog : UserControl
{
    private readonly MainViewModel? _vm;

    public ConfirmDialog(string title, string message, MainViewModel? vm = null)
    {
        InitializeComponent();
        _vm = vm;
        TitleTb.Text   = title;
        MessageTb.Text = message;
        Opacity = 0;
        Loaded += (_, _) => AnimateOpen();
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.24);

        var translate = new TranslateTransform(0, 20);
        var scale = new ScaleTransform(0.98, 0.98);
        var group = new TransformGroup();
        group.Children.Add(scale);
        group.Children.Add(translate);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = group;

        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, dur) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(20, 0, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.98, 1, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.98, 1, dur) { EasingFunction = ease });
    }

    private void CloseForm(bool result)
    {
        if (_vm != null) _vm.Modal.Close(result);
        else if (DataContext is MainViewModel vm) vm.Modal.Close(result);
        else (Application.Current.MainWindow?.DataContext as MainViewModel)?.Modal.Close(result);
    }

    private void Yes_Click(object sender, RoutedEventArgs e) => CloseForm(true);
    private void No_Click(object sender, RoutedEventArgs e)  => CloseForm(false);
}
