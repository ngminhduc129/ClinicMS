using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();

        Opacity = 0;
        Loaded += (_, _) => AnimateFadeIn();

        vm.Modal.Register(ModalOverlay, ModalHost);
    }

    private void AnimateFadeIn()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.4);

        var anim = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        Timeline.SetDesiredFrameRate(anim, 60);
        anim.Freeze();

        BeginAnimation(OpacityProperty, anim);
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
