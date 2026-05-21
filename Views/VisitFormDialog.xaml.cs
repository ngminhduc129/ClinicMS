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
    private List<string> _doctorNames = new();
    private List<string> _medicineNames = new();
    private List<string> _serviceNames = new();
    private bool _isEditing => _editing != null;

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
            RefreshDoctorList();
            RefreshMedicineList();
            RefreshServiceList();

            // Subscribe to changes
            _vm.DoctorVM.Doctors.CollectionChanged += OnDoctorsChanged;
            _vm.MedicineVM.Medicines.CollectionChanged += OnMedicinesChanged;
            _vm.ServiceVM.Services.CollectionChanged += OnServicesChanged;
        };

        Unloaded += (_, _) =>
        {
            _vm.DoctorVM.Doctors.CollectionChanged -= OnDoctorsChanged;
            _vm.MedicineVM.Medicines.CollectionChanged -= OnMedicinesChanged;
            _vm.ServiceVM.Services.CollectionChanged -= OnServicesChanged;
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        };
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Note: These properties are now in MainViewModel but were restored by the generalist
        if (e.PropertyName == "FullDoctorNames")
            RefreshDoctorList();
    }

    private void OnDoctorsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshDoctorList();
    private void OnMedicinesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshMedicineList();
    private void OnServicesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshServiceList();

    private void RefreshDoctorList()
    {
        _doctorNames = _vm.FullDoctorNames.Keys.OrderBy(n => n).ToList();
    }

    private void RefreshMedicineList()
    {
        _medicineNames = _vm.MedicineVM.Medicines.Select(m => m.MedicineName ?? "").ToList();
    }

    private void RefreshServiceList()
    {
        _serviceNames = _vm.ServiceVM.Services.Select(s => s.ServiceName ?? "").ToList();
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
            MedicineNameBox.Focus();
        }
    }

    private void CancelAddMedicine_Click(object sender, RoutedEventArgs e)
    {
        MedicineNameBox.Text = "";
        MedicineQty.Clear();
        MedicineDosage.Clear();
        MedicineInputArea.Visibility = Visibility.Collapsed;
    }

    private void ConfirmAddMedicine_Click(object sender, RoutedEventArgs e)
    {
        try 
        {
            var medicineName = MedicineNameBox.Text.Trim();
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
            MedicineNameBox.Text = "";
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
            ServiceNameBox.Focus();
        }
    }

    private void CancelAddService_Click(object sender, RoutedEventArgs e)
    {
        ServiceNameBox.Text = "";
        ServiceResultBox.Clear();
        ServiceInputArea.Visibility = Visibility.Collapsed;
    }

    private void ConfirmAddService_Click(object sender, RoutedEventArgs e)
    {
        try 
        {
            var serviceName = ServiceNameBox.Text.Trim();
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
            ServiceNameBox.Text = "";
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
            PatientSuggestPopup.IsOpen = false;
            DoctorSuggestPopup.IsOpen = false;
            ServiceSuggestPopup.IsOpen = false;
            MedicineSuggestPopup.IsOpen = false;

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
        PatientSuggestPopup.IsOpen = false;
        DoctorSuggestPopup.IsOpen = false;
        ServiceSuggestPopup.IsOpen = false;
        MedicineSuggestPopup.IsOpen = false;
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
        PatientSuggestPopup.IsOpen = false;
        DoctorSuggestPopup.IsOpen = false;
        ServiceSuggestPopup.IsOpen = false;
        MedicineSuggestPopup.IsOpen = false;
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

    private void Overlay_Click(object s, MouseButtonEventArgs e)
    {
        PatientSuggestPopup.IsOpen = false;
        DoctorSuggestPopup.IsOpen = false;
        ServiceSuggestPopup.IsOpen = false;
        MedicineSuggestPopup.IsOpen = false;
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

    // ───────────────────────────── Patient search (name + phone) ─────────────────────────────
    private bool _isSelectingPatient;
    private int _patientSearchCount;

    private async void PatientNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isSelectingPatient) return;
            int currentRequest = ++_patientSearchCount;
            await Task.Delay(300);
            if (currentRequest != _patientSearchCount) return;

            var searchTerm = PatientNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                PatientSuggestPopup.IsOpen = false;
                return;
            }

            var patients = await _vm.PatientVM.QuickSearchAsync(searchTerm);
            PatientSuggestList.ItemsSource = patients;
            PatientSuggestPopup.IsOpen = patients is { Count: > 0 };
        }
        catch
        {
            PatientSuggestPopup.IsOpen = false;
        }
    }

    private async void PatientSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (PatientSuggestList.SelectedItem is not Patient selected) return;

            _isSelectingPatient = true;
            _patientSearchCount++; // invalidate any pending debounce
            PatientNameBox.Text = selected.FullName ?? "";
            PatientNameBox.CaretIndex = PatientNameBox.Text.Length;
            _isSelectingPatient = false;

            _selectedPatientId = selected.PatientId;
            PatientSuggestPopup.IsOpen = false;
        }
        catch
        {
            _isSelectingPatient = false;
            PatientSuggestPopup.IsOpen = false;
        }
    }

    private void PatientNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        PatientSuggestPopup.IsOpen = false;
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (PresentationSource.FromDependencyObject(e.OriginalSource as DependencyObject) != PresentationSource.FromDependencyObject(this))
                return;

            PatientSuggestPopup.IsOpen = false;
            DoctorSuggestPopup.IsOpen = false;
            MedicineSuggestPopup.IsOpen = false;
            ServiceSuggestPopup.IsOpen = false;

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    // ───────────────────────────── Doctor search ─────────────────────────────
    private int _doctorSearchCount;

    private async void DoctorNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isSuppressTextChanged) return;
            int currentRequest = ++_doctorSearchCount;
            await Task.Delay(300);
            if (currentRequest != _doctorSearchCount) return;

            _selectedDoctorId = null;
            var searchTerm = DoctorNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                DoctorSuggestPopup.IsOpen = false;
                return;
            }

            var filtered = _doctorNames
                .Where(n => n.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(MaxSuggestionItems)
                .ToList();

            DoctorSuggestList.ItemsSource = filtered;
            DoctorSuggestPopup.IsOpen = filtered.Count > 0;
        }
        catch
        {
            DoctorSuggestPopup.IsOpen = false;
        }
    }

    private bool _isSuppressTextChanged;

    private void DoctorSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DoctorSuggestList.SelectedItem is not string selected) return;

        _doctorSearchCount++;
        _isSuppressTextChanged = true;
        DoctorNameBox.Text = selected;
        DoctorNameBox.CaretIndex = DoctorNameBox.Text.Length;
        _isSuppressTextChanged = false;

        if (_vm.FullDoctorNames.TryGetValue(selected, out var did))
            _selectedDoctorId = did;
        DoctorSuggestPopup.IsOpen = false;
    }

    private void DoctorNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        DoctorSuggestPopup.IsOpen = false;
    }

    // ───────────────────────────── Service search ─────────────────────────────
    private int _serviceSearchCount;

    private async void ServiceNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isSuppressTextChanged) return;
            int currentRequest = ++_serviceSearchCount;
            await Task.Delay(300);
            if (currentRequest != _serviceSearchCount) return;

            var searchTerm = ServiceNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ServiceSuggestPopup.IsOpen = false;
                return;
            }

            var filtered = _serviceNames
                .Where(n => n.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(MaxSuggestionItems)
                .ToList();

            ServiceSuggestList.ItemsSource = filtered;
            ServiceSuggestPopup.IsOpen = filtered.Count > 0;
        }
        catch
        {
            ServiceSuggestPopup.IsOpen = false;
        }
    }

    private void ServiceSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ServiceSuggestList.SelectedItem is not string selected) return;

        _serviceSearchCount++;
        _isSuppressTextChanged = true;
        ServiceNameBox.Text = selected;
        ServiceNameBox.CaretIndex = ServiceNameBox.Text.Length;
        _isSuppressTextChanged = false;
        ServiceSuggestPopup.IsOpen = false;
    }

    private void ServiceNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ServiceSuggestPopup.IsOpen = false;
    }

    // ───────────────────────────── Medicine search ─────────────────────────────
    private int _medicineSearchCount;

    private async void MedicineNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isSuppressTextChanged) return;
            int currentRequest = ++_medicineSearchCount;
            await Task.Delay(300);
            if (currentRequest != _medicineSearchCount) return;

            var searchTerm = MedicineNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                MedicineSuggestPopup.IsOpen = false;
                return;
            }

            var filtered = _medicineNames
                .Where(n => n.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Take(MaxSuggestionItems)
                .ToList();

            MedicineSuggestList.ItemsSource = filtered;
            MedicineSuggestPopup.IsOpen = filtered.Count > 0;
        }
        catch
        {
            MedicineSuggestPopup.IsOpen = false;
        }
    }

    private void MedicineSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MedicineSuggestList.SelectedItem is not string selected) return;

        _medicineSearchCount++;
        _isSuppressTextChanged = true;
        MedicineNameBox.Text = selected;
        MedicineNameBox.CaretIndex = MedicineNameBox.Text.Length;
        _isSuppressTextChanged = false;
        MedicineSuggestPopup.IsOpen = false;
    }

    private void MedicineNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        MedicineSuggestPopup.IsOpen = false;
    }
}
