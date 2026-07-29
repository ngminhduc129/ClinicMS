using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using ClinicMS.Models;
using ClinicMS.Services;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class AppointmentFormDialog : UserControl
{
    private readonly MainViewModel _vm;

    public AppointmentFormDialog(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Opacity = 0;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object s, RoutedEventArgs e)
    {
        try
        {
        AnimateOpen();

        ApptDatePicker.SelectedDate = DateTime.Today;

        if (_vm.PatientVM.Patients.Count == 0)
            await _vm.PatientVM.LoadAsync();
        PatientCombo.ItemsSource = _vm.PatientVM.Patients
            .Select(p => $"{p.FullName} ({p.Phone ?? "N/A"})")
            .ToList();

        if (_vm.DoctorVM.Doctors.Count == 0)
            await _vm.DoctorVM.LoadAsync();
            DoctorCombo.ItemsSource = _vm.DoctorVM.Doctors
                .Select(d => d.FullName)
                .ToList();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error in AppointmentFormDialog.OnLoaded");
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi tải form: {ex.Message}", "Lỗi", true));
        }
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.2);

        var translate = new TranslateTransform(0, 15);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = translate;

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        var animTranslate = new DoubleAnimation(15, 0, dur) { EasingFunction = ease };

        Timeline.SetDesiredFrameRate(animOpacity, 60);
        Timeline.SetDesiredFrameRate(animTranslate, 60);

        animOpacity.Freeze();
        animTranslate.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
        translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
    }

    private void CloseForm(bool? result = null) => _vm.Modal.Close(result);

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var patientText = PatientCombo.Text?.Trim();
            var doctorText = DoctorCombo.Text?.Trim();

            if (string.IsNullOrWhiteSpace(patientText))
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng chọn bệnh nhân.", "Thiếu thông tin", true));
                return;
            }

            if (string.IsNullOrWhiteSpace(doctorText))
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng chọn bác sĩ.", "Thiếu thông tin", true));
                return;
            }

            var selectedDate = ApptDatePicker.SelectedDate;
            if (selectedDate == null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng chọn ngày hẹn.", "Thiếu thông tin", true));
                return;
            }

            var timeText = TimeBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(timeText))
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập giờ hẹn.", "Thiếu thông tin", true));
                return;
            }

            var patient = _vm.PatientVM.Patients.FirstOrDefault(p =>
                $"{p.FullName} ({p.Phone ?? "N/A"})".Equals(patientText, StringComparison.OrdinalIgnoreCase) ||
                p.FullName.Equals(patientText, StringComparison.OrdinalIgnoreCase));

            if (patient == null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Không tìm thấy bệnh nhân này trong hệ thống.", "Thông báo", true));
                return;
            }

            var doctor = _vm.DoctorVM.Doctors.FirstOrDefault(d =>
                d.FullName.Equals(doctorText, StringComparison.OrdinalIgnoreCase));

            if (doctor == null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Không tìm thấy bác sĩ này trong hệ thống.", "Thông báo", true));
                return;
            }

            var appt = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = doctor.DoctorId,
                AppointmentTime = $"{selectedDate:yyyy-MM-dd} {timeText}",
                Status = "Scheduled",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var confirm = new ConfirmDialog(
                "Xác nhận tạo lịch hẹn?",
                $"Bệnh nhân: {patient.FullName}\nBác sĩ: {doctor.FullName}\nThời gian: {appt.AppointmentTime}",
                _vm);

            _vm.Modal.Show(confirm, async result =>
            {
                if (result == true)
                {
                    try
                    {
                        SaveBtn.IsEnabled = false;
                        var service = App.ServiceProvider.GetRequiredService<Services.DashboardService>();
                        await service.CreateAppointmentAsync(appt);
                        CloseForm(true);
                    }
                    catch (Exception ex)
                    {
                        SaveBtn.IsEnabled = true;
                        _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi tạo lịch hẹn: {ex.Message}", "Lỗi hệ thống", true));
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi: {ex.Message}", "Lỗi hệ thống", true));
        }
    }

    private void ToggleInlineAdd_Click(object s, RoutedEventArgs e)
    {
        InlineAddContainer.Visibility = InlineAddContainer.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (InlineAddContainer.Visibility == Visibility.Visible)
        {
            InlineNameBox.Text = "";
            InlinePhoneBox.Text = "";
            InlineNameBox.Focus();
        }
    }

    private async void InlineSave_Click(object s, RoutedEventArgs e)
    {
        var name = InlineNameBox.Text?.Trim();
        var phone = InlinePhoneBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            InlineNameBox.Focus();
            return;
        }

        try
        {
            var service = App.ServiceProvider.GetRequiredService<PatientService>();
            var newPatient = await service.AddPatientAsync(new Patient { FullName = name, Phone = phone ?? "" });

            _vm.PatientVM.Patients.Add(newPatient);
            PatientCombo.ItemsSource = _vm.PatientVM.Patients
                .Select(p => $"{p.FullName} ({p.Phone ?? "N/A"})")
                .ToList();
            PatientCombo.Text = $"{newPatient.FullName} ({newPatient.Phone ?? "N/A"})";

            InlineAddContainer.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi thêm bệnh nhân: {ex.Message}", "Lỗi hệ thống", true));
        }
    }

    private void InlineCancel_Click(object s, RoutedEventArgs e)
    {
        InlineAddContainer.Visibility = Visibility.Collapsed;
    }

    private void Cancel_Click(object s, RoutedEventArgs e) => CloseForm();

    private void Close_Click(object s, RoutedEventArgs e) => CloseForm();

    private void Overlay_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.Source == e.OriginalSource || (e.OriginalSource is FrameworkElement fe && fe.Name != "DialogRoot" && FindParent<Border>(fe)?.Name != "DialogRoot"))
        {
            CloseForm();
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }
}