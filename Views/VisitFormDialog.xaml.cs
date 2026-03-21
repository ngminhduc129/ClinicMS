using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class VisitFormDialog : UserControl
{
    private const int MaxSuggestionItems = 25;

    private readonly MainViewModel _vm;
    private readonly Visit? _editing;
    private List<string> _patientNames = new();
    private List<string> _doctorNames = new();
    private bool _isEditing => _editing != null;
    private bool _searchSetupDone;

    public VisitFormDialog(MainViewModel vm, Visit? visit = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = visit;
        Opacity = 0;

        if (_isEditing && visit != null)
            LoadData(visit);
        else
            VisitDatePicker.SelectedDate = DateTime.Today;

        // Xử lý chống giật lag khi mở form: Cho hiệu ứng bay ra trước, Load data sau
        Loaded += async (_, _) =>
        {
            AnimateOpen();
            HookDateBorderFocus();

            // Chờ 150ms để animation mượt mà hoàn tất
            await Task.Delay(150);

            RefreshPatientList();
            RefreshDoctorList();

            if (!_searchSetupDone)
            {
                SetupSearchComboBox(PatientNameBox, _patientNames);
                SetupSearchComboBox(DoctorNameBox, _doctorNames);
                _searchSetupDone = true;
            }

            // Subscribe to changes to handle data loading after dialog opens
            _vm.Patients.CollectionChanged += OnPatientsChanged;
            _vm.Doctors.CollectionChanged += OnDoctorsChanged;
        };

        Unloaded += (_, _) =>
        {
            _vm.Patients.CollectionChanged -= OnPatientsChanged;
            _vm.Doctors.CollectionChanged -= OnDoctorsChanged;
        };
    }

    private void OnPatientsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshPatientList();
    private void OnDoctorsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshDoctorList();

    private void RefreshPatientList()
    {
        _patientNames = BuildNameList(_vm.Patients.Select(p => p.FullName));
        UpdateComboSource(PatientNameBox, _patientNames);
    }

    private void RefreshDoctorList()
    {
        _doctorNames = BuildNameList(_vm.Doctors.Select(d => d.FullName));
        UpdateComboSource(DoctorNameBox, _doctorNames);
    }

    private static void UpdateComboSource(ComboBox comboBox, List<string> items)
    {
        comboBox.Tag = items;
        if (comboBox.ItemsSource != items)
            comboBox.ItemsSource = items;
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

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, dur) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(40, 0, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
    }

    private void CloseForm(bool? result = null)
    {
        if (DataContext is MainViewModel vm) vm.Modal.Close(result);
        else _vm.Modal.Close(result);
    }

    // ───────────────────────────── DateBorder focus effect ─────────────────────────────

    private static readonly SolidColorBrush _dateBorderNormal = new(Color.FromRgb(229, 231, 235));
    private static readonly SolidColorBrush _dateBorderFocused = new(Color.FromRgb(22, 163, 74));
    private static readonly SolidColorBrush _dateBorderHover = new(Color.FromRgb(134, 239, 172));

    private void HookDateBorderFocus()
    {
        VisitDatePicker.GotFocus += (_, _) =>
        {
            DateBorder.BorderBrush = _dateBorderFocused;
            DateBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            { Color = Color.FromRgb(34, 197, 94), Opacity = 0.20, BlurRadius = 10, ShadowDepth = 0 };
        };
        VisitDatePicker.LostFocus += (_, _) =>
        {
            DateBorder.BorderBrush = _dateBorderNormal;
            DateBorder.Effect = null;
        };
        DateBorder.MouseEnter += (_, _) =>
        {
            if (!VisitDatePicker.IsFocused) DateBorder.BorderBrush = _dateBorderHover;
        };
        DateBorder.MouseLeave += (_, _) =>
        {
            if (!VisitDatePicker.IsFocused) DateBorder.BorderBrush = _dateBorderNormal;
        };
    }

    // ───────────────────────────── Data ─────────────────────────────

    private void LoadData(Visit v)
    {
        PatientNameBox.Text = v.Patient?.FullName ?? _vm.Patients.FirstOrDefault(p => p.PatientId == v.PatientId)?.FullName ?? "";
        DoctorNameBox.Text = v.Doctor?.FullName ?? _vm.Doctors.FirstOrDefault(d => d.DoctorId == v.DoctorId)?.FullName ?? "";

        if (DateTime.TryParse(v.VisitTime, out var dt))
            VisitDatePicker.SelectedDate = dt;

        SymptomsBox.Text = v.Symptoms ?? "";
        DiagnosisBox.Text = v.Diagnosis ?? "";
        NoteBox.Text = v.Note ?? "";
        PulseBox.Text = v.Pulse ?? "";
        BPBox.Text = v.BloodPressure ?? "";
        TempBox.Text = v.Temperature ?? "";
        PrescriptionBox.Text = v.PrescriptionText ?? "";

        // ĐÃ SỬA: Load dữ liệu cho RadioButton Loại Khám
        RbAppt.IsChecked = v.VisitType == "Appointment";
        RbEmerg.IsChecked = v.VisitType == "Emergency";
        RbFollow.IsChecked = v.VisitType == "Follow-up";
        if (RbAppt.IsChecked == false && RbEmerg.IsChecked == false && RbFollow.IsChecked == false)
            RbWalkIn.IsChecked = true;

        // ĐÃ SỬA: Load dữ liệu cho RadioButton Trạng Thái
        RbProgress.IsChecked = v.Status == "In Progress";
        RbComplete.IsChecked = v.Status == "Completed";
        if (RbProgress.IsChecked == false && RbComplete.IsChecked == false)
            RbWaiting.IsChecked = true;
    }

    private Visit BuildVisit(int patientId, int doctorId)
    {
        var dt = BuildVisitDateTime(VisitDatePicker.SelectedDate, _editing);
        return new Visit
        {
            VisitId = _editing?.VisitId ?? 0,
            PatientId = patientId,
            DoctorId = doctorId,
            VisitTime = dt.ToString("yyyy-MM-dd HH:mm:ss"),

            // ĐÃ SỬA: Lấy dữ liệu từ RadioButton
            VisitType = RbAppt.IsChecked == true ? "Appointment" :
                        RbEmerg.IsChecked == true ? "Emergency" :
                        RbFollow.IsChecked == true ? "Follow-up" : "Walk-in",

            Status = RbProgress.IsChecked == true ? "In Progress" :
                     RbComplete.IsChecked == true ? "Completed" : "Waiting",

            Symptoms = SymptomsBox.Text.Trim(),
            Diagnosis = DiagnosisBox.Text.Trim(),
            Note = NoteBox.Text.Trim(),
            Pulse = PulseBox.Text.Trim(),
            BloodPressure = BPBox.Text.Trim(),
            Temperature = TempBox.Text.Trim(),
            PrescriptionText = PrescriptionBox.Text.Trim()
        };
    }

    private static DateTime BuildVisitDateTime(DateTime? selectedDate, Visit? editing)
    {
        var date = (selectedDate ?? DateTime.Today).Date;

        if (editing != null && DateTime.TryParse(editing.VisitTime, out var oldDateTime))
        {
            // Giữ lại giờ cũ khi sửa; nếu giờ cũ là 00:00 thì dùng giờ hiện tại.
            var oldTime = oldDateTime.TimeOfDay;
            return date.Add(oldTime == TimeSpan.Zero ? DateTime.Now.TimeOfDay : oldTime);
        }

        // Bản ghi mới lấy giờ hiện tại để không bị mặc định 00:00.
        return date.Add(DateTime.Now.TimeOfDay);
    }

    // ───────────────────────────── Validation ─────────────────────────────

    private bool TryResolvePatientId(out int patientId, out string error)
    {
        patientId = 0; error = "";
        var name = PatientNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { error = "Vui lòng nhập tên bệnh nhân."; return false; }
        var matches = _vm.Patients.Where(p => string.Equals(p.FullName?.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) { error = "Không tìm thấy bệnh nhân trong hệ thống."; return false; }
        if (matches.Count > 1) { error = "Có nhiều bệnh nhân trùng tên, vui lòng nhập đúng tên đầy đủ."; return false; }
        patientId = matches[0].PatientId; return true;
    }

    private bool TryResolveDoctorId(out int doctorId, out string error)
    {
        doctorId = 0; error = "";
        var name = DoctorNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { error = "Vui lòng nhập tên bác sĩ."; return false; }
        var matches = _vm.Doctors.Where(d => string.Equals(d.FullName?.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) { error = "Không tìm thấy bác sĩ trong hệ thống."; return false; }
        if (matches.Count > 1) { error = "Có nhiều bác sĩ trùng tên, vui lòng nhập đúng tên đầy đủ."; return false; }
        doctorId = matches[0].DoctorId; return true;
    }

    // ───────────────────────────── Button events ─────────────────────────────

    public bool IsDirty()
    {
        if (!_isEditing)
        {
            return !string.IsNullOrWhiteSpace(PatientNameBox.Text) ||
                   !string.IsNullOrWhiteSpace(DoctorNameBox.Text) ||
                   !string.IsNullOrWhiteSpace(SymptomsBox.Text) ||
                   !string.IsNullOrWhiteSpace(DiagnosisBox.Text) ||
                   !string.IsNullOrWhiteSpace(NoteBox.Text) ||
                   !string.IsNullOrWhiteSpace(PulseBox.Text) ||
                   !string.IsNullOrWhiteSpace(BPBox.Text) ||
                   !string.IsNullOrWhiteSpace(TempBox.Text) ||
                   !string.IsNullOrWhiteSpace(PrescriptionBox.Text);
        }

        if (_editing == null) return false;

        string oldPatient = _editing.Patient?.FullName ?? _vm.Patients.FirstOrDefault(p => p.PatientId == _editing.PatientId)?.FullName ?? "";
        bool patientChanged = PatientNameBox.Text.Trim() != oldPatient;

        string oldDoctor = _editing.Doctor?.FullName ?? _vm.Doctors.FirstOrDefault(d => d.DoctorId == _editing.DoctorId)?.FullName ?? "";
        bool doctorChanged = DoctorNameBox.Text.Trim() != oldDoctor;

        string oldTime = _editing.VisitTime ?? "";
        string newTime = VisitDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
        // So sánh chỉ ngày cho đơn giản (vì DatePicker không có giờ)
        bool timeChanged = !oldTime.StartsWith(newTime) && !string.IsNullOrEmpty(newTime);

        bool symptomsChanged = SymptomsBox.Text.Trim() != (_editing.Symptoms ?? "");
        bool diagnosisChanged = DiagnosisBox.Text.Trim() != (_editing.Diagnosis ?? "");
        bool noteChanged = NoteBox.Text.Trim() != (_editing.Note ?? "");
        bool pulseChanged = PulseBox.Text.Trim() != (_editing.Pulse ?? "");
        bool bpChanged = BPBox.Text.Trim() != (_editing.BloodPressure ?? "");
        bool tempChanged = TempBox.Text.Trim() != (_editing.Temperature ?? "");
        bool prescriptionChanged = PrescriptionBox.Text.Trim() != (_editing.PrescriptionText ?? "");

        string oldType = _editing.VisitType ?? "Walk-in";
        string newType = RbAppt.IsChecked == true ? "Appointment" :
                         RbEmerg.IsChecked == true ? "Emergency" :
                         RbFollow.IsChecked == true ? "Follow-up" : "Walk-in";
        bool typeChanged = oldType != newType;

        string oldStatus = _editing.Status ?? "Waiting";
        string newStatus = RbProgress.IsChecked == true ? "In Progress" :
                          RbComplete.IsChecked == true ? "Completed" : "Waiting";
        bool statusChanged = oldStatus != newStatus;

        return patientChanged || doctorChanged || timeChanged || symptomsChanged || diagnosisChanged ||
               noteChanged || pulseChanged || bpChanged || tempChanged || prescriptionChanged ||
               typeChanged || statusChanged;
    }

    // ───────────────────────────── Button events ─────────────────────────────

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        if (!TryResolvePatientId(out var patientId, out var patientError))
        { MessageBox.Show(patientError, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (!TryResolveDoctorId(out var doctorId, out var doctorError))
        { MessageBox.Show(doctorError, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var v = BuildVisit(patientId, doctorId);
        bool ok = _isEditing ? await _vm.UpdateVisitAsync(v) : await _vm.AddVisitAsync(v);
        if (ok) CloseForm(true);
    }

    private void Cancel_Click(object s, RoutedEventArgs e)
    {
        if (IsDirty())
        {
            var result = MessageBox.Show(
                "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?",
                "Hủy thay đổi?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }
        CloseForm();
    }

    private void Close_Click(object s, RoutedEventArgs e)
    {
        if (IsDirty())
        {
            var result = MessageBox.Show(
                "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?",
                "Thoát form?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }
        CloseForm();
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    public bool HasData() => IsDirty();

    private static List<string> BuildNameList(IEnumerable<string?> names) =>
        names.Select(n => n?.Trim()).Where(n => !string.IsNullOrWhiteSpace(n))
             .Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();

    private static void SetupSearchComboBox(ComboBox comboBox, List<string> allItems)
    {
        comboBox.Tag = allItems;
        comboBox.ItemsSource = allItems;
        comboBox.IsTextSearchEnabled = false; // Bắt buộc false để không loạn Text khi gõ

        comboBox.Loaded += (_, _) =>
        {
            if (comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox tb)
            {
                tb.TextChanged -= EditableComboBox_TextChanged;
                tb.TextChanged += EditableComboBox_TextChanged;
            }
        };
    }

    private static async void EditableComboBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;
        if (cb.Tag is not List<string> sourceItems) return;

        // Lưu vị trí con trỏ và text hiện tại
        var text = tb.Text ?? string.Empty;
        var caretIndex = tb.CaretIndex;

        // Tránh vòng lặp vô tận khi cập nhật ItemsSource
        tb.TextChanged -= EditableComboBox_TextChanged;

        try 
        {
            // Lọc dữ liệu từ danh sách GỐC (sourceItems)
            var filtered = string.IsNullOrWhiteSpace(text)
                ? sourceItems
                : sourceItems.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(MaxSuggestionItems).ToList();

            // Cập nhật nguồn dữ liệu
            cb.ItemsSource = filtered;
            
            // Chỉ mở dropdown nếu có kết quả và đang focus
            cb.IsDropDownOpen = filtered.Any() && tb.IsFocused;

            // Khôi phục lại text và con trỏ (vì gán ItemsSource có thể làm mất text)
            tb.Text = text;
            cb.Text = text;
            tb.CaretIndex = caretIndex;
        }
        finally
        {
            tb.TextChanged += EditableComboBox_TextChanged;
        }
    }

    private void SearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb || cb.SelectedItem is not string selected) return;

        cb.Text = selected;
        cb.IsDropDownOpen = false;

        if (cb.Template.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.CaretIndex = tb.Text.Length;
        }
    }

    private void SearchComboBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ComboBox cb) cb.IsDropDownOpen = false;
    }

    private void Overlay_Click(object s, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not Grid g || g.Name != "Overlay") return;
        
        if (!IsDirty())
        {
            CloseForm();
            return;
        }

        var result = MessageBox.Show(
            "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?",
            "Thoát form?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) CloseForm();
    }
}
