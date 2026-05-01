using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class DoctorDetailDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Doctor        _doctor;

    public DoctorDetailDialog(MainViewModel vm, Doctor doctor)
    {
        InitializeComponent();
        _vm     = vm;
        _doctor = doctor;
        Opacity = 0;
        LoadData();
        Loaded += (_, _) => AnimateOpen();
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.4);

        var translate = new TranslateTransform(0, 40);
        var scale = new ScaleTransform(0.96, 0.96);
        var group = new TransformGroup();
        group.Children.Add(scale);
        group.Children.Add(translate);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = group;

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        var animTranslate = new DoubleAnimation(40, 0, dur) { EasingFunction = ease };
        var animScaleX = new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease };
        var animScaleY = new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease };

        Timeline.SetDesiredFrameRate(animOpacity, 60);
        Timeline.SetDesiredFrameRate(animTranslate, 60);
        Timeline.SetDesiredFrameRate(animScaleX, 60);
        Timeline.SetDesiredFrameRate(animScaleY, 60);

        animOpacity.Freeze();
        animTranslate.Freeze();
        animScaleX.Freeze();
        animScaleY.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
        translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
    }

    private void LoadData()
    {
        var d = _doctor;
        InitialsText.Text  = d.Initials;
        NameText.Text      = d.FullName;
        SpecialtyText.Text = d.Specialty ?? "";
        IdRow.Value        = d.DisplayId;
        PhoneRow.Value     = d.Phone     ?? "—";
        EmailRow.Value     = d.Email     ?? "—";
        SpecRow.Value      = d.Specialty ?? "—";

        if (!string.IsNullOrEmpty(d.Note))
        {
            NoteSection.Visibility = Visibility.Visible;
            NoteText.Text = d.Note;
        }
        else
        {
            NoteSection.Visibility = Visibility.Collapsed;
        }
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        var form = new DoctorFormDialog(_vm, _doctor);
        _vm.Modal.Show(form, result => { if (result == true) LoadData(); }, () => !form.HasData());
    }

    private void Close_Click(object s, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.Modal.Close();
        else _vm.Modal.Close();
    }
}
