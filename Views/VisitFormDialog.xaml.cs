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
using Microsoft.EntityFrameworkCore;
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

    // Danh sách dịch vụ chỉ định
    private readonly System.Collections.ObjectModel.ObservableCollection<ServiceOrder> _serviceOrders = new();

    public VisitFormDialog(MainViewModel vm, Visit? visit = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = visit;
        Opacity = 0;

        // Kết nối ListView với danh sách thuốc và dịch vụ
        PrescriptionList.ItemsSource = _prescriptionItems;
        MedicineInputArea.Visibility = Visibility.Collapsed;

        ServiceOrderListDialog.ItemsSource = _serviceOrders;
        ServiceInputArea.Visibility = Visibility.Collapsed;

        if (_isEditing && visit != null)
            LoadData(visit);
        else
            VisitDatePicker.SelectedDate = DateTime.Today;

        Loaded += (_, _) =>
        {
            AnimateOpen();
            HookDateBorderFocus();

            // Tải trước dữ liệu tĩnh khi control đã sẵn sàng trong visual tree
            RefreshPatientList();
            RefreshDoctorList();
            RefreshMedicineList();
            RefreshServiceList();

            if (!_searchSetupDone)
            {
                SetupSearchComboBox(PatientNameBox, _patientNames);
                SetupSearchComboBox(DoctorNameBox, _doctorNames);
                SetupSearchComboBox(MedicineCombo, _vm.MedicineVM.Medicines.Select(m => m.MedicineName ?? "").ToList());
                SetupSearchComboBox(ServiceCombo, _vm.ServiceVM.Services.Select(s => s.ServiceName ?? "").ToList());
                _searchSetupDone = true;
            }

            // Subscribe to changes
            _vm.PatientVM.Patients.CollectionChanged += OnPatientsChanged;
            _vm.DoctorVM.Doctors.CollectionChanged += OnDoctorsChanged;
            _vm.MedicineVM.Medicines.CollectionChanged += OnMedicinesChanged;
            _vm.ServiceVM.Services.CollectionChanged += OnServicesChanged;
        };

        Unloaded += (_, _) =>
        {
            _vm.PatientVM.Patients.CollectionChanged -= OnPatientsChanged;
            _vm.DoctorVM.Doctors.CollectionChanged -= OnDoctorsChanged;
            _vm.MedicineVM.Medicines.CollectionChanged -= OnMedicinesChanged;
            _vm.ServiceVM.Services.CollectionChanged -= OnServicesChanged;
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
    private void OnServicesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshServiceList();

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

    private void RefreshServiceList()
    {
        var services = _vm.ServiceVM.Services.Select(s => s.ServiceName ?? "").ToList();
        UpdateComboSource(ServiceCombo, services);
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
        var dur = TimeSpan.FromSeconds(0.2);

        var translate = new TranslateTransform(0, 15);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = translate;

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        var animTranslate = new DoubleAnimation(15, 0, dur) { EasingFunction = ease };
        
        // Optional: Hint WPF to target higher framerate for this specific animation
        Timeline.SetDesiredFrameRate(animOpacity, 60);
        Timeline.SetDesiredFrameRate(animTranslate, 60);

        // Freeze animation to improve performance
        animOpacity.Freeze();
        animTranslate.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
        translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
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
        var detailList = details.ToList();
        v.PrescriptionDetails = detailList; // Đồng bộ để IsDirty có thể so sánh chính xác

        _prescriptionItems.Clear();
        foreach (var d in detailList) _prescriptionItems.Add(d);
        
        // Ensure the list reflects the loaded items
        PrescriptionList.ItemsSource = _prescriptionItems;

        // Load dịch vụ chỉ định
        var serviceOrders = await _vm.VisitVM.GetServiceOrdersAsync(v.VisitId);
        var orderList = serviceOrders.ToList();
        v.ServiceOrders = orderList; // Đồng bộ để IsDirty có thể so sánh chính xác

        _serviceOrders.Clear();
        foreach (var o in orderList) _serviceOrders.Add(o);

        // Ensure the list reflects the loaded items
        ServiceOrderListDialog.ItemsSource = _serviceOrders;

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

        foreach (var item in _serviceOrders)
        {
            v.ServiceOrders.Add(new ServiceOrder
            {
                VisitId = v.VisitId,
                ServiceId = item.ServiceId,
                Result = item.Result
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

        // Nếu người dùng chỉ nhập tên, kiểm tra xem có bao nhiêu người trùng tên
        await using var db = new ClinicMS.Data.ClinicDbContext();
        var matches = await db.Patients.Where(p => p.FullName == name).ToListAsync();

        if (matches.Count == 0) return (0, "Không tìm thấy bệnh nhân trong hệ thống.");
        if (matches.Count > 1) return (0, $"Có {matches.Count} bệnh nhân tên '{name}'. Vui lòng chọn chính xác từ danh sách gợi ý (có kèm mã ID).");
        
        return (matches[0].PatientId, null);
    }

    private async Task<(int id, string? error)> TryResolveDoctorIdAsync()
    {
        if (_selectedDoctorId.HasValue) return (_selectedDoctorId.Value, null);

        var name = DoctorNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return (0, "Vui lòng nhập tên bác sĩ.");

        if (_vm.FullDoctorNames.TryGetValue(name, out var id))
            return (id, null);

        await using var db = new ClinicMS.Data.ClinicDbContext();
        var matches = await db.Doctors.Where(d => d.FullName == name).ToListAsync();

        if (matches.Count == 0) return (0, "Không tìm thấy bác sĩ trong hệ thống.");
        if (matches.Count > 1) return (0, $"Có {matches.Count} bác sĩ tên '{name}'. Vui lòng chọn chính xác từ danh sách gợi ý.");
        
        return (matches[0].DoctorId, null);
    }
    // ───────────────────────────── Prescription Logic ─────────────────────────────

    private void AddMedicine_Click(object sender, RoutedEventArgs e)
    {
        MedicineInputArea.Visibility = MedicineInputArea.Visibility == Visibility.Visible 
            ? Visibility.Collapsed : Visibility.Visible;
        if (MedicineInputArea.Visibility == Visibility.Visible)
        {
            MedicineCombo.ApplyTemplate();
            MedicineCombo.Focus();
        }
    }

    private void CancelAddMedicine_Click(object sender, RoutedEventArgs e)
    {
        MedicineCombo.Text = "";
        MedicineQty.Clear();
        MedicineDosage.Clear();
        MedicineInputArea.Visibility = Visibility.Collapsed;
    }

    private void ConfirmAddMedicine_Click(object sender, RoutedEventArgs e)
    {
        try 
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

            // KIỂM TRA TỒN KHO
            if (med.AvailableQuantity <= 0)
            {
                _vm.Modal.Show(new MessageDialog(_vm, $"Thuốc '{med.MedicineName}' đã hết hoặc đã bị giữ chỗ hết.", "Hết hàng", true));
                return;
            }

            if (string.IsNullOrWhiteSpace(MedicineQty.Text))
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập số lượng.", "Thông báo", true));
                return;
            }

            double.TryParse(MedicineQty.Text, out double qty);
            if (qty <= 0) qty = 1;

            if (qty > med.AvailableQuantity)
            {
                _vm.Modal.Show(new MessageDialog(_vm, $"Số lượng kê ({qty}) vượt quá số lượng khả dụng hiện có ({med.AvailableQuantity}).", "Cảnh báo tồn kho", true));
                return;
            }

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
        catch (Exception ex)
        {
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi thêm thuốc: {ex.Message}", "Lỗi", true));
        }
    }

    private void RemoveMedicine_Click(object sender, RoutedEventArgs e)
    {
        try 
        {
            if (sender is Button btn && btn.Tag is PrescriptionDetail detail)
            {
                _prescriptionItems.Remove(detail);
            }
        }
        catch (Exception ex)
        {
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi xóa thuốc: {ex.Message}", "Lỗi", true));
        }
    }

    // ───────────────────────────── Service Orders Logic ─────────────────────────────

    private void AddService_Click(object sender, RoutedEventArgs e)
    {
        ServiceInputArea.Visibility = ServiceInputArea.Visibility == Visibility.Visible 
            ? Visibility.Collapsed : Visibility.Visible;
        if (ServiceInputArea.Visibility == Visibility.Visible)
        {
            ServiceCombo.ApplyTemplate();
            ServiceCombo.Focus();
        }
    }

    private void CancelAddService_Click(object sender, RoutedEventArgs e)
    {
        ServiceCombo.Text = "";
        ServiceResultBox.Clear();
        ServiceInputArea.Visibility = Visibility.Collapsed;
    }

    private void ConfirmAddService_Click(object sender, RoutedEventArgs e)
    {
        try 
        {
            var serviceName = ServiceCombo.Text.Trim();
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng chọn dịch vụ.", "Thông báo", true));
                return;
            }

            var svc = _vm.ServiceVM.Services.FirstOrDefault(s => s.ServiceName?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true);
            if (svc == null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Không tìm thấy dịch vụ này trong hệ thống.", "Thông báo", true));
                return;
            }

            // Kiểm tra xem dịch vụ đã có trong danh sách chỉ định chưa
            var existing = _serviceOrders.FirstOrDefault(s => s.ServiceId == svc.ServiceId);
            if (existing != null)
            {
                _vm.Modal.Show(new MessageDialog(_vm, "Dịch vụ này đã được chỉ định.", "Thông báo", true));
                return;
            }

            _serviceOrders.Add(new ServiceOrder
            {
                ServiceId = svc.ServiceId,
                Service = svc,
                Result = ServiceResultBox.Text.Trim()
            });

            // Reset input
            ServiceCombo.Text = "";
            ServiceResultBox.Clear();
            ServiceInputArea.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi thêm dịch vụ: {ex.Message}", "Lỗi", true));
        }
    }

    private void RemoveService_Click(object sender, RoutedEventArgs e)
    {
        try 
        {
            if (sender is Button btn && btn.Tag is ServiceOrder order)
            {
                _serviceOrders.Remove(order);
            }
        }
        catch (Exception ex)
        {
            _vm.Modal.Show(new MessageDialog(_vm, $"Lỗi khi xóa dịch vụ: {ex.Message}", "Lỗi", true));
        }
    }

    // ───────────────────────────── Button events ─────────────────────────────

    public bool IsDirty()
    {
        if (!_isEditing)
        {
            // Trường hợp tạo mới: Kiểm tra xem có bất kỳ dữ liệu nào được nhập chưa
            return !string.IsNullOrWhiteSpace(PatientNameBox.Text) ||
                   !string.IsNullOrWhiteSpace(DoctorNameBox.Text) ||
                   !string.IsNullOrWhiteSpace(SymptomsBox.Text) ||
                   !string.IsNullOrWhiteSpace(DiagnosisBox.Text) ||
                   !string.IsNullOrWhiteSpace(NoteBox.Text) ||
                   !string.IsNullOrWhiteSpace(PulseBox.Text) ||
                   !string.IsNullOrWhiteSpace(BPBox.Text) ||
                   !string.IsNullOrWhiteSpace(TempBox.Text) ||
                   !string.IsNullOrWhiteSpace(PrescriptionBox.Text) ||
                   _prescriptionItems.Any() ||
                   RbAppt.IsChecked == true || RbEmerg.IsChecked == true || RbFollow.IsChecked == true ||
                   RbProgress.IsChecked == true || RbComplete.IsChecked == true ||
                   (VisitDatePicker.SelectedDate != null && VisitDatePicker.SelectedDate != DateTime.Today);
        }

        if (_editing == null) return false;

        // Trường hợp chỉnh sửa: So sánh giá trị hiện tại với dữ liệu gốc
        bool patientChanged = PatientNameBox.Text != (_editing.Patient?.FullName ?? _vm.PatientVM.Patients.FirstOrDefault(p => p.PatientId == _editing.PatientId)?.FullName ?? "");
        bool doctorChanged = DoctorNameBox.Text != (_editing.Doctor?.FullName ?? _vm.DoctorVM.Doctors.FirstOrDefault(d => d.DoctorId == _editing.DoctorId)?.FullName ?? "");
        
        DateTime.TryParse(_editing.VisitTime, out var originalDate);
        bool dateChanged = VisitDatePicker.SelectedDate?.Date != originalDate.Date;

        bool typeChanged = (RbAppt.IsChecked == true && _editing.VisitType != "Appointment") ||
                          (RbEmerg.IsChecked == true && _editing.VisitType != "Emergency") ||
                          (RbFollow.IsChecked == true && _editing.VisitType != "Follow-up") ||
                          (RbWalkIn.IsChecked == true && _editing.VisitType != "Walk-in" && !string.IsNullOrEmpty(_editing.VisitType));

        bool statusChanged = (RbProgress.IsChecked == true && _editing.Status != "In Progress") ||
                            (RbComplete.IsChecked == true && _editing.Status != "Completed") ||
                            (RbWaiting.IsChecked == true && _editing.Status != "Waiting" && !string.IsNullOrEmpty(_editing.Status));

        // Kiểm tra chi tiết đơn thuốc
        bool prescriptionChanged = _prescriptionItems.Count != _editing.PrescriptionDetails.Count;
        if (!prescriptionChanged && _prescriptionItems.Any())
        {
            foreach (var item in _prescriptionItems)
            {
                var original = _editing.PrescriptionDetails.FirstOrDefault(pd => pd.MedicineId == item.MedicineId);
                if (original == null || Math.Abs(original.Quantity - item.Quantity) > 0.0001 || original.Dosage != item.Dosage)
                {
                    prescriptionChanged = true;
                    break;
                }
            }
        }

        // Kiểm tra chi tiết dịch vụ chỉ định
        bool servicesChanged = false;
        if (_editing != null)
        {
            servicesChanged = _serviceOrders.Count != (_editing.ServiceOrders?.Count ?? 0);
            if (!servicesChanged && _serviceOrders.Any() && _editing.ServiceOrders != null)
            {
                foreach (var item in _serviceOrders)
                {
                    var original = _editing.ServiceOrders.FirstOrDefault(so => so.ServiceId == item.ServiceId);
                    if (original == null || original.Result != item.Result)
                    {
                        servicesChanged = true;
                        break;
                    }
                }
            }
        }
        else
        {
            servicesChanged = _serviceOrders.Any();
        }

        return patientChanged || doctorChanged || dateChanged || typeChanged || statusChanged ||
               SymptomsBox.Text != (_editing?.Symptoms ?? "") ||
               DiagnosisBox.Text != (_editing?.Diagnosis ?? "") ||
               NoteBox.Text != (_editing?.Note ?? "") ||
               PulseBox.Text != (_editing?.Pulse ?? "") ||
               BPBox.Text != (_editing?.BloodPressure ?? "") ||
               TempBox.Text != (_editing?.Temperature ?? "") ||
               PrescriptionBox.Text != (_editing?.PrescriptionText ?? "") ||
               prescriptionChanged ||
               servicesChanged;
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
                _vm.Modal.Show(new MessageDialog(_vm, "Không thể lưu hồ sơ khám. Vui lòng kiểm tra lại thông tin và kết nối.", "Lỗi lưu dữ liệu", true));
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
                ? sourceItems.Take(MaxSuggestionItems).ToList()
                : sourceItems.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(MaxSuggestionItems).ToList();

            cb.ItemsSource = filtered;
            
            if (filtered.Any() && tb.IsFocused)
            {
                cb.IsDropDownOpen = true;
                // Force open dropdown if it closed due to items change
                await cb.Dispatcher.InvokeAsync(() => {
                    if (tb.IsFocused && filtered.Any()) cb.IsDropDownOpen = true;
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
            else
            {
                cb.IsDropDownOpen = false;
            }

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
        if (e.Source == e.OriginalSource || (e.OriginalSource is FrameworkElement fe && fe.Name != "DialogRoot" && FindParent<Border>(fe)?.Name != "DialogRoot"))
        {
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

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (PresentationSource.FromDependencyObject(e.OriginalSource as DependencyObject) != PresentationSource.FromDependencyObject(this))
                return;

            PatientNameBox.IsDropDownOpen = false;
            DoctorNameBox.IsDropDownOpen = false;
            MedicineCombo.IsDropDownOpen = false;
            ServiceCombo.IsDropDownOpen = false;

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
