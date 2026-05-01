using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class PatientDetailDialog : UserControl
{
    private readonly PatientDetailViewModel _viewModel;

    public PatientDetailDialog(MainViewModel mainVm, Patient patient)
    {
        InitializeComponent();
        _viewModel = new PatientDetailViewModel(mainVm, patient);
        DataContext = _viewModel;

        Opacity = 0;
        Loaded += async (_, _) => 
        {
            AnimateOpen();
            await _viewModel.LoadDataAsync();
        };
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.4);

        var translate = new TranslateTransform(0, 30);
        var group = new TransformGroup();
        group.Children.Add(translate);
        
        DialogRoot.RenderTransformOrigin = new Point(0.5, 0.5);
        DialogRoot.RenderTransform = group;

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        var animTranslate = new DoubleAnimation(30, 0, dur) { EasingFunction = ease };

        Timeline.SetDesiredFrameRate(animOpacity, 60);
        Timeline.SetDesiredFrameRate(animTranslate, 60);

        animOpacity.Freeze();
        animTranslate.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
        translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        _viewModel.Edit();
    }

    private void Close_Click(object s, RoutedEventArgs e)
    {
        _viewModel.Close();
    }
}
