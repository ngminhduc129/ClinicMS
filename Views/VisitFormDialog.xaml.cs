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
                SetupSearchComboBox(MedicineCombo, _vm.MedicineVM.Medicines.Select(m => m.MedicineName ?? "").ToList());
                _searchSetupDone = true;
            }

            // Subscribe to changes to handle data loading after dialog opens
            _vm.PatientVM.Patients.CollectionChanged += OnPatientsChanged;
            _vm.DoctorVM.Doctors.CollectionChanged += OnDoctorsChanged;
            _vm.MedicineVM.Medicines.CollectionChanged += OnMedicinesChanged;
        };

        Unloaded += (_, _) =>
        {
            _vm.PatientVM.Patients.CollectionChanged -= OnPatientsChanged;
            _vm.DoctorVM.Doctors.CollectionChanged -= OnDoctorsChanged;
            _vm.MedicineVM.Medicines.CollectionChanged -= OnMedicinesChanged;
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        };
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Note: These properties are now in MainViewModel but were restored by the generalist
        if (e.PropertyName == "FullPatientNames")
            RefreshPatientList();
        else if (e.PropertyName == "FullDoctorNames")
            RefreshDoctorList();
    }

    private void OnPatientsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshPatientList();
    private void OnDoctorsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshDoctorList();
    private void OnMedicinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshMedicineList();

    private void RefreshPatientList()
    {
        _patientNames = _vm.FullPatientNames.Keys.OrderBy(n => n).ToList();
        UpdateComboSource(PatientNameBox, _patientNames);
    }

    private void RefreshDoctorList()
    {
        _doctorNames = _vm.FullDoctorNames.Keys.OrderBy(n => n).ToList();
        UpdateComboSource(DoctorNameBox, _doctorNames);
    }

    private void RefreshMedicineList()
    {
        var medicines = _vm.MedicineVM.Medicines.Select(m => m.MedicineName ?? "").ToList();
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
        _vm.Modal.Close(result);
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

        PatientNameBox.Text = v.Patient?.FullName ?? _vm.PatientVM.Patients.FirstOrDefault(p => p.PatientId == v.PatientId)?.FullName ?? "";
        DoctorNameBox.Text = v.Doctor?.FullName ?? _vm.DoctorVM.Doctors.FirstOrDefault(d => d.DoctorId == v.DoctorId)?.FullName ?? "";

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
        var details = await _vm.VisitVM.GetPrescriptionDetailsAsync(v.VisitId);
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

        // Ưu tiên lookup từ danh sách đầy đủ (có kèm ID)
        if (_vm.FullPatientNames.TryGetValue(name, out var id))
            return (id, null);

        var match = await _vm.Db.GetPatientByNameAsync(name);

        if (match == null) return (0, "Không tìm thấy bệnh nhân trong hệ thống.");
        return (match.PatientId, null);
    }

    private async Task<(int id, string? error)> TryResolveDoctorIdAsync()
    {
        if (_selectedDoctorId.HasValue) return (_selectedDoctorId.Value, null);

        var name = DoctorNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return (0, "Vui lòng nhập tên bác sĩ.");

        if (_vm.FullDoctorNames.TryGetValue(name, out var id))
            return (id, null);

        var match = await _vm.Db.GetDoctorByNameAsync(name);

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
            _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng chọn thuốc.", "Thông báo", true));
            return;
        }

        var med = _vm.MedicineVM.Medicines.FirstOrDefault(m => m.MedicineName?.Equals(medicineName, StringComparison.OrdinalIgnoreCase) == true);
        if (med == null)
        {
            _vm.Modal.Show(new MessageDialog(_vm, "Không tìm thấy thuốc này trong hệ thống.", "Thông báo", true));
            return;
        }

        if (string.IsNullOrWhiteSpace(MedicineQty.Text))
        {
            _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập số lượng.", "Thông báo", true));
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
        return true; 
    }

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        try
        {
            var (patientId, patientError) = await TryResolvePatientIdAsync();
            if (patientError != null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, patientError, "Thiếu thông tin", true));
                return;
            }

            var (doctorId, doctorError) = await TryResolveDoctorIdAsync();
            if (doctorError != null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, doctorError, "Thiếu thông tin", true));
                return;
            }

            SaveBtn.IsEnabled = false;
            var v = BuildVisit(patientId, doctorId);
            
            // Lưu Visit và đơn thuốc đi kèm
            bool ok = _isEditing ? await _vm.VisitVM.UpdateAsync(v) : await _vm.VisitVM.AddAsync(v);
            
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
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi lưu hồ sơ: {ex.Message}", "Lỗi hệ thống", true));
        }
    }

    private void Cancel_Click(object s, RoutedEventArgs e)
    {
        if (IsDirty())
        {
            _vm.Modal.Show(new ConfirmDialog("Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?", "Hủy thay đổi?", _vm), (res) => {
                if (res == true) CloseForm();
            });
        }
        else CloseForm();
    }

    private void Close_Click(object s, RoutedEventArgs e)
    {
        if (IsDirty())
        {
            _vm.Modal.Show(new ConfirmDialog("Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?", "Thoát form?", _vm), (res) => {
                if (res == true) CloseForm();
            });
        }
        else CloseForm();
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

    private static void SetupSearchComboBox(ComboBox comboBox, List<string> allItems)
    {
        comboBox.Tag = allItems;
        comboBox.ItemsSource = allItems;
        comboBox.IsTextSearchEnabled = false;

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

        var text = tb.Text ?? string.Empty;
        var caretIndex = tb.CaretIndex;

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

        tb.TextChanged -= EditableComboBox_TextChanged;

        try 
        {
            var filtered = string.IsNullOrWhiteSpace(text)
                ? sourceItems
                : sourceItems.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(MaxSuggestionItems).ToList();

            cb.ItemsSource = filtered;
            cb.IsDropDownOpen = filtered.Any() && tb.IsFocused;

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

        if (cb.Name == "PatientNameBox")
        {
            if (_vm.FullPatientNames.TryGetValue(selected, out var pid))
                _selectedPatientId = pid;
        }
        else if (cb.Name == "DoctorNameBox")
        {
            if (_vm.FullDoctorNames.TryGetValue(selected, out var did))
                _selectedDoctorId = did;
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

        _vm.Modal.Show(new ConfirmDialog("Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?", "Thoát form?", _vm), (res) => {
            if (res == true) CloseForm();
        });
    }
}
