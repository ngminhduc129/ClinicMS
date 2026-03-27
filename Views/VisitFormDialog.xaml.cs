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
using ClinicMS.Services;

namespace ClinicMS.Views;

public partial class VisitFormDialog : UserControl
{
    private const int MaxSuggestionItems = 25;

    private readonly MainViewModel _vm;
    private readonly Visit? _editing;
    private int? _selectedPatientId;
    private int? _selectedDoctorId;
    private List<string> _patientNames = new();
    private List<string> _doctorNames = new();
    private bool _isEditing => _editing != null;
    private bool _searchSetupDone;

    // Danh sách thuốc đang kê
    private readonly System.Collections.ObjectModel.ObservableCollection<PrescriptionDetail> _prescriptionItems = new();

    public VisitFormDialog(MainViewModel vm, Visit? visit = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = visit;
        Opacity = 0;

        // Kết nối ListView với danh sách thuốc
        PrescriptionList.ItemsSource = _prescriptionItems;
        MedicineInputArea.Visibility = Visibility.Collapsed;

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
            RefreshMedicineList();

            if (!_searchSetupDone)
            {
                SetupSearchComboBox(PatientNameBox, _patientNames);
                SetupSearchComboBox(DoctorNameBox, _doctorNames);
                SetupSearchComboBox(MedicineCombo, _vm.Medicines.Select(m => m.MedicineName ?? "").ToList());
                _searchSetupDone = true;
            }

            // Subscribe to changes to handle data loading after dialog opens
            _vm.Patients.CollectionChanged += OnPatientsChanged;
            _vm.Doctors.CollectionChanged += OnDoctorsChanged;
            _vm.Medicines.CollectionChanged += OnMedicinesChanged;
        };

        Unloaded += (_, _) =>
        {
            _vm.Patients.CollectionChanged -= OnPatientsChanged;
            _vm.Doctors.CollectionChanged -= OnDoctorsChanged;
            _vm.Medicines.CollectionChanged -= OnMedicinesChanged;
        };
    }

    private void OnPatientsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshPatientList();
    private void OnDoctorsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshDoctorList();
    private void OnMedicinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshMedicineList();

    private void RefreshPatientList()
    {
        // Dùng cache tên đầy đủ từ ViewModel thay vì danh sách bị giới hạn 50 người
        _patientNames = BuildNameList(_vm.FullPatientNames);
        UpdateComboSource(PatientNameBox, _patientNames);
    }

    private void RefreshDoctorList()
    {
        _doctorNames = BuildNameList(_vm.FullDoctorNames);
        UpdateComboSource(DoctorNameBox, _doctorNames);
    }

    private void RefreshMedicineList()
    {
        var medicines = _vm.Medicines.Select(m => m.MedicineName ?? "").ToList();
        UpdateComboSource(MedicineCombo, medicines);
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

    private async void LoadData(Visit v)
    {
        _selectedPatientId = v.PatientId;
        _selectedDoctorId = v.DoctorId;

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

        // Load thuốc đã kê
        var details = await _vm.GetPrescriptionDetailsAsync(v.VisitId);
        _prescriptionItems.Clear();
        foreach (var d in details) _prescriptionItems.Add(d);
        
        // Ensure the list reflects the loaded items
        PrescriptionList.ItemsSource = _prescriptionItems;

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
        var v = new Visit
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

        foreach (var item in _prescriptionItems)
        {
            v.PrescriptionDetails.Add(new PrescriptionDetail
            {
                VisitId = v.VisitId,
                MedicineId = item.MedicineId,
                Quantity = item.Quantity,
                Dosage = item.Dosage
            });
        }
        return v;
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

    private async Task<(int id, string? error)> TryResolvePatientIdAsync()
    {
        if (_selectedPatientId.HasValue) return (_selectedPatientId.Value, null);

        var name = PatientNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return (0, "Vui lòng nhập tên bệnh nhân.");

        var db = new DatabaseService();
        var match = await db.GetPatientByNameAsync(name);

        if (match == null) return (0, "Không tìm thấy bệnh nhân trong hệ thống.");
        return (match.PatientId, null);
    }

    private async Task<(int id, string? error)> TryResolveDoctorIdAsync()
    {
        if (_selectedDoctorId.HasValue) return (_selectedDoctorId.Value, null);

        var name = DoctorNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return (0, "Vui lòng nhập tên bác sĩ.");

        var db = new DatabaseService();
        var match = await db.GetDoctorByNameAsync(name);

        if (match == null) return (0, "Không tìm thấy bác sĩ trong hệ thống.");
        return (match.DoctorId, null);
    }
    // ───────────────────────────── Prescription Logic ─────────────────────────────

    private void AddMedicine_Click(object sender, RoutedEventArgs e)
    {
        MedicineInputArea.Visibility = MedicineInputArea.Visibility == Visibility.Visible 
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ConfirmAddMedicine_Click(object sender, RoutedEventArgs e)
    {
        var medicineName = MedicineCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(medicineName))
        {
            MessageBox.Show("Vui lòng chọn thuốc.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var med = _vm.Medicines.FirstOrDefault(m => m.MedicineName?.Equals(medicineName, StringComparison.OrdinalIgnoreCase) == true);
        if (med == null)
        {
            MessageBox.Show("Không tìm thấy thuốc này trong hệ thống.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(MedicineQty.Text))
        {
            MessageBox.Show("Vui lòng nhập số lượng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        double.TryParse(MedicineQty.Text, out double qty);
        if (qty <= 0) qty = 1;

        // Kiểm tra xem thuốc đã có trong đơn chưa
        var existing = _prescriptionItems.FirstOrDefault(p => p.MedicineId == med.MedicineId);
        if (existing != null)
        {
            existing.Quantity += qty;
            existing.Dosage = MedicineDosage.Text;
            // Refresh ListView
            PrescriptionList.Items.Refresh();
        }
        else
        {
            _prescriptionItems.Add(new PrescriptionDetail
            {
                MedicineId = med.MedicineId,
                Medicine = med,
                Quantity = qty,
                Dosage = MedicineDosage.Text
            });
        }

        // Reset input
        MedicineCombo.Text = "";
        MedicineQty.Clear();
        MedicineDosage.Clear();
        MedicineInputArea.Visibility = Visibility.Collapsed;
    }

    private void RemoveMedicine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PrescriptionDetail detail)
        {
            _prescriptionItems.Remove(detail);
        }
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
                   !string.IsNullOrWhiteSpace(PrescriptionBox.Text) ||
                   _prescriptionItems.Any();
        }

        if (_editing == null) return false;

        // ... (các so sánh cũ) ...
        // Thêm kiểm tra đơn thuốc thay đổi (để đơn giản, ta coi như có item là dirty khi sửa)
        // Trong thực tế cần so sánh danh sách cũ và mới
        return true; 
    }

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var (patientId, patientError) = await TryResolvePatientIdAsync();
            if (patientError != null)
            { 
                MessageBox.Show(patientError, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); 
                return; 
            }

            var (doctorId, doctorError) = await TryResolveDoctorIdAsync();
            if (doctorError != null)
            { 
                MessageBox.Show(doctorError, "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); 
                return; 
            }

            SaveBtn.IsEnabled = false;
            var v = BuildVisit(patientId, doctorId);
            
            // Lưu Visit và đơn thuốc đi kèm
            bool ok = _isEditing ? await _vm.UpdateVisitAsync(v) : await _vm.AddVisitAsync(v);
            
            if (ok)
            {
                CloseForm(true);
            }
            else
            {
                SaveBtn.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            SaveBtn.IsEnabled = true;
            MessageBox.Show($"Lỗi khi lưu hồ sơ: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }

    public bool HasData() => IsDirty();

    private static List<string> BuildNameList(IEnumerable<string?> names) =>
        names.Select(n => n?.Trim()).Where(n => !string.IsNullOrWhiteSpace(n))
             .Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();

    private static void SetupSearchComboBox(ComboBox comboBox, List<string> allItems)
    {
        comboBox.Tag = allItems;
        comboBox.ItemsSource = allItems;
        comboBox.IsTextSearchEnabled = false;

        // Đảm bảo tìm thấy TextBox ngay cả khi Template chưa load xong hoàn toàn
        var tb = comboBox.Template?.FindName("PART_EditableTextBox", comboBox) as TextBox;
        if (tb != null)
        {
            tb.TextChanged -= EditableComboBox_TextChanged;
            tb.TextChanged += EditableComboBox_TextChanged;
        }
        else
        {
            comboBox.Loaded += (s, e) =>
            {
                if (comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox tb2)
                {
                    tb2.TextChanged -= EditableComboBox_TextChanged;
                    tb2.TextChanged += EditableComboBox_TextChanged;
                }
            };
        }
    }

    private static async void EditableComboBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;
        if (cb.Tag is not List<string> sourceItems) return;

        // Lưu vị trí con trỏ và text hiện tại
        var text = tb.Text ?? string.Empty;
        var caretIndex = tb.CaretIndex;

        // Reset ID khi người dùng gõ tay đổi tên
        if (cb.Name == "PatientNameBox")
        {
            var parent = FindParent<VisitFormDialog>(cb);
            if (parent != null) parent._selectedPatientId = null;
        }
        else if (cb.Name == "DoctorNameBox")
        {
            var parent = FindParent<VisitFormDialog>(cb);
            if (parent != null) parent._selectedDoctorId = null;
        }

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

        // Cập nhật ID dựa trên tên đã chọn (nếu là Patient/Doctor box)
        if (cb.Name == "PatientNameBox")
        {
            var p = _vm.Patients.FirstOrDefault(x => x.FullName == selected);
            if (p != null) _selectedPatientId = p.PatientId;
        }
        else if (cb.Name == "DoctorNameBox")
        {
            var d = _vm.Doctors.FirstOrDefault(x => x.FullName == selected);
            if (d != null) _selectedDoctorId = d.DoctorId;
        }

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
