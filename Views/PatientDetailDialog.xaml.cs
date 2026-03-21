using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class PatientDetailDialog : UserControl
{
    private readonly MainViewModel _vm;
    private Patient _patient;

    public PatientDetailDialog(MainViewModel vm, Patient patient)
    {
        InitializeComponent();
        _vm      = vm;
        _patient = patient;
        Opacity = 0;
        _ = LoadAsync();
        Loaded += (_, _) => AnimateOpen();
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.24);

        var translate = new TranslateTransform(0, 40);
        var scale = new ScaleTransform(0.96, 0.96);
        var group = new TransformGroup();
        group.Children.Add(scale);
        group.Children.Add(translate);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = group;

        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, dur) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(40, 0, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
    }

    private async Task LoadAsync()
    {
        var (p, contact) = await _vm.GetPatientWithContactAsync(_patient.PatientId);
        if (p == null) { Close_Click(null!, null!); return; }
        _patient = p;

        InitialsCircle.Text = p.Initials;
        NameText.Text       = p.FullName;
        GenderDobText.Text  = $"{p.Gender ?? "—"}  •  {p.DateOfBirth ?? "—"}";

        PhoneRow.Value   = contact?.Phone   ?? "—";
        EmailRow.Value   = contact?.Email   ?? "—";
        AddressRow.Value = contact?.Address ?? "—";

        BloodRow.Value   = p.BloodType ?? "—";
        HeightRow.Value  = p.HeightCm.HasValue  ? $"{p.HeightCm} cm" : "—";
        WeightRow.Value  = p.WeightKg.HasValue  ? $"{p.WeightKg} kg" : "—";
        NoteRow.Value    = p.Note ?? "—";

        var visits = await _vm.GetPatientVisitsAsync(p.PatientId);
        VisitList.ItemsSource = visits;
        NoVisitsText.Visibility = visits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Edit_Click(object s, RoutedEventArgs e)
    {
        var form = new PatientFormDialog(_vm, _patient);
        _vm.Modal.Show(form, result => { if (result == true) _ = LoadAsync(); }, () => form.HasData());
    }

    private void Close_Click(object s, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.Modal.Close();
        else _vm.Modal.Close();
    }
}
