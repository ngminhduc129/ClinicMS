using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class VisitListView : UserControl
{
    private MainViewModel? _vm;
    private Visit? _selectedVisit;
    private string _currentStatus = "Waiting";
    private bool _isDirty = false;
    private DateTime? _selectedDateFilter;

    // Collection phục vụ DataTemplate cho danh sách thuốc
    public ObservableCollection<PrescriptionDetail> SelectedPrescriptionDetails { get; } = new();

    public VisitListView()
    {
        InitializeComponent();
        
        // MẶC ĐỊNH: Chọn lọc ngày hôm nay khi mở tab
        _selectedDateFilter = DateTime.Today;
        VisitDateFilter.SelectedDate = _selectedDateFilter;

        DataContextChanged += OnDataContextChanged;

        VisitListCard.SizeChanged += (s, e) =>
            VisitListCard.Clip = new RectangleGeometry(
                new Rect(0, 0, VisitListCard.ActualWidth, VisitListCard.ActualHeight), 16, 16);

        DetailCard.SizeChanged += (s, e) =>
            DetailCard.Clip = new RectangleGeometry(
                new Rect(0, 0, DetailCard.ActualWidth, DetailCard.ActualHeight), 16, 16);
    }

    private async void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            
            // Đồng bộ bộ lọc ngày hiện tại trên UI vào ViewModel
            if (VisitDateFilter.SelectedDate.HasValue)
            {
                _vm.VisitVM.VisitDateFilter = VisitDateFilter.SelectedDate.Value.ToString("yyyy-MM-dd");
            }

            if (_vm.VisitVM.Visits.Count == 0) await _vm.VisitVM.LoadAsync();
        }
    }

    private async void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange > 0 && e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
        {
            if (_vm != null)
            {
                await _vm.VisitVM.LoadMoreAsync();
            }
        }
    }

    private int _searchRequestCount = 0;
    private async void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        if (_vm == null) return;
        var term = ((TextBox)s).Text.Trim();
        int currentRequest = ++_searchRequestCount;
        await Task.Delay(300); 
        if (currentRequest == _searchRequestCount)
        {
            await _vm.VisitVM.SearchAsync(term);
        }
    }

    private void VisitGroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VisitGroupList.SelectedItem is Visit v)
        {
            if (_isDirty && _selectedVisit != null && _selectedVisit.VisitId != v.VisitId)
            {
                if (_vm != null)
                {
                    _vm.Modal.Show(new ConfirmDialog(
                        "Bạn có thay đổi chưa lưu. Bạn có chắc muốn chuyển sang bệnh nhân khác và mất các thay đổi này không?",
                        "Xác nhận chuyển", _vm), (res) =>
                    {
                        if (res == true)
                        {
                            _selectedVisit = v;
                            _isDirty = false;
                            ShowDetail(v);
                        }
                        else
                        {
                            VisitGroupList.SelectionChanged -= VisitGroupList_SelectionChanged;
                            VisitGroupList.SelectedItem = _selectedVisit;
                            VisitGroupList.SelectionChanged += VisitGroupList_SelectionChanged;
                        }
                    });
                    return;
                }
            }

            _selectedVisit = v;
            _isDirty = false;
            ShowDetail(v);
        }
    }

    private async void ShowDetail(Visit v)
    {
        _selectedVisit = v;
        _isDirty = false;
        DetailEmpty.Visibility = Visibility.Collapsed;
        DetailLoading.Visibility = Visibility.Collapsed;
        DetailForm.Visibility = Visibility.Visible;

        DetailAvatar.Text = !string.IsNullOrEmpty(v.PatientName) ? v.PatientName[0].ToString().ToUpper() : "?";
        DetailPatientName.Text = v.PatientName;
        DetailPatientGender.Text = $"Bác sĩ: {v.DoctorName}";
        DetailPatientId.Text = $"Ngày: {FormatVisitDate(v.VisitTime)}";
        DetailPatientAge.Text = $"Loại: {v.VisitTypeDisplay}";

        InputPulse.Text = v.Pulse ?? "";
        InputBloodPressure.Text = v.BloodPressure ?? "";
        InputTemperature.Text = v.Temperature ?? "";
        InputWeight.Text = v.Weight ?? "";
        InputSymptoms.Text = v.Symptoms ?? "";
        InputNote.Text = v.Note ?? "";
        InputDiagnosis.Text = v.Diagnosis ?? "";

        _currentStatus = NormalizeStatus(v.Status);
        UpdateStatusButtons();

        SelectedPrescriptionDetails.Clear();
        if (_vm != null)
        {
            var details = await _vm.VisitVM.GetPrescriptionDetailsAsync(v.VisitId);
            if (details.Any())
            {
                MedicineEmpty.Visibility = Visibility.Collapsed;
                foreach (var d in details) SelectedPrescriptionDetails.Add(d);
            }
            else
            {
                MedicineEmpty.Visibility = Visibility.Visible;
            }
        }
    }

    private void UpdateStatusButtons()
    {
        Style btnActive = (Style)FindResource("StatusBtn"); // Sử dụng style chung
        
        // Reset colors
        BtnWaiting.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
        BtnInProgress.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
        BtnCompleted.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));

        // Highlight active
        if (_currentStatus == "Waiting") BtnWaiting.BorderBrush = Brushes.Orange;
        else if (_currentStatus == "In Progress") BtnInProgress.BorderBrush = Brushes.Blue;
        else if (_currentStatus == "Completed") BtnCompleted.BorderBrush = Brushes.Green;
    }

    private void StatusWaiting_Click(object s, RoutedEventArgs e) { _currentStatus = "Waiting"; UpdateStatusButtons(); _isDirty = true; }
    private void StatusInProgress_Click(object s, RoutedEventArgs e) { _currentStatus = "In Progress"; UpdateStatusButtons(); _isDirty = true; }
    private void StatusCompleted_Click(object s, RoutedEventArgs e) { _currentStatus = "Completed"; UpdateStatusButtons(); _isDirty = true; }

    private async void RemoveMedicineDetail_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || _selectedVisit == null || sender is not Button btn || btn.Tag is not PrescriptionDetail detail) return;
        var currentDetails = await _vm.VisitVM.GetPrescriptionDetailsAsync(_selectedVisit.VisitId);
        var toRemove = currentDetails.FirstOrDefault(d => d.MedicineId == detail.MedicineId);
        if (toRemove != null)
        {
            currentDetails.Remove(toRemove);
            await _vm.VisitVM.SavePrescriptionAsync(_selectedVisit.VisitId, currentDetails);
            ShowDetail(_selectedVisit);
            _vm.Toast.Show("Đã xóa thuốc khỏi đơn.");
        }
    }

    private void AddMedicineDetail_Click(object sender, RoutedEventArgs e) 
    { 
        QuickAddMedicineArea.Visibility = Visibility.Visible;
        if (_vm != null)
        {
            var medicines = _vm.MedicineVM.Medicines.Select(m => m.MedicineName ?? "").ToList();
            SetupSearchComboBox(QuickMedicineCombo, medicines);
        }
    }

    private void CancelQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        QuickAddMedicineArea.Visibility = Visibility.Collapsed;
        QuickMedicineCombo.Text = "";
        QuickMedicineQty.Text = "1";
        QuickMedicineDosage.Text = "";
    }

    private async void ConfirmQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || _selectedVisit == null) return;
        var medicineName = QuickMedicineCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(medicineName)) return;
        var med = _vm.MedicineVM.Medicines.FirstOrDefault(m => m.MedicineName?.Equals(medicineName, StringComparison.OrdinalIgnoreCase) == true);
        if (med == null) { _vm.Toast.ShowError("Không tìm thấy thuốc này."); return; }
        double.TryParse(QuickMedicineQty.Text, out double qty);
        if (qty <= 0) qty = 1;
        var newItem = new PrescriptionDetail { VisitId = _selectedVisit.VisitId, MedicineId = med.MedicineId, Medicine = med, Quantity = qty, Dosage = QuickMedicineDosage.Text.Trim() };
        var currentDetails = await _vm.VisitVM.GetPrescriptionDetailsAsync(_selectedVisit.VisitId);
        var existing = currentDetails.FirstOrDefault(d => d.MedicineId == newItem.MedicineId);
        if (existing != null) { existing.Quantity += newItem.Quantity; existing.Dosage = newItem.Dosage; }
        else currentDetails.Add(newItem);
        await _vm.VisitVM.SavePrescriptionAsync(_selectedVisit.VisitId, currentDetails);
        CancelQuickAdd_Click(null!, null!);
        ShowDetail(_selectedVisit); 
        _vm.Toast.Show("Đã thêm thuốc vào đơn.");
    }

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || _selectedVisit == null) return;
        _selectedVisit.Status = _currentStatus;
        _selectedVisit.Pulse = InputPulse.Text.Trim(); 
        _selectedVisit.BloodPressure = InputBloodPressure.Text.Trim(); 
        _selectedVisit.Temperature = InputTemperature.Text.Trim(); 
        _selectedVisit.Weight = InputWeight.Text.Trim(); 
        _selectedVisit.Symptoms = InputSymptoms.Text.Trim(); 
        _selectedVisit.Note = InputNote.Text.Trim(); 
        _selectedVisit.Diagnosis = InputDiagnosis.Text.Trim();
        await _vm.VisitVM.UpdateAsync(_selectedVisit);
        _isDirty = false;
    }

    private void CancelDetail_Click(object s, RoutedEventArgs e) { _selectedVisit = null; DetailForm.Visibility = Visibility.Collapsed; DetailEmpty.Visibility = Visibility.Visible; _isDirty = false; }
    private string FormatVisitDate(string? time) => DateTime.TryParse(time, out var dt) ? dt.ToString("dd/MM/yyyy HH:mm") : (time ?? "--");
    private string NormalizeStatus(string? status) => string.IsNullOrEmpty(status) ? "Waiting" : status;

    private async void Refresh_Click(object s, RoutedEventArgs e) { if (_vm != null) await Task.WhenAll(_vm.PatientVM.LoadAsync(), _vm.DoctorVM.LoadAsync(), _vm.VisitVM.LoadAsync()); }
    private async void VisitDateFilter_SelectedDateChanged(object s, SelectionChangedEventArgs e)
    {
        if (_vm == null) return;
        await _vm.VisitVM.SearchByDateAsync(VisitDateFilter.SelectedDate?.ToString("yyyy-MM-dd"));
    }
    private void FilterToday_Click(object s, RoutedEventArgs e) { VisitDateFilter.SelectedDate = DateTime.Today; }
    private void FilterAllDate_Click(object s, RoutedEventArgs e) { VisitDateFilter.SelectedDate = null; }

    private void Add_Click(object s, RoutedEventArgs e) { if (_vm == null) return; var form = new VisitFormDialog(_vm); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.VisitVM.LoadAsync(); }, () => form.HasData()); }
    private void Edit_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var form = new VisitFormDialog(_vm, _selectedVisit); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.VisitVM.LoadAsync(); }, () => form.HasData()); }
    private void Delete_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var confirm = new ConfirmDialog("Xóa hồ sơ khám này?", "Thao tác không thể hoàn tác.", _vm); _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.VisitVM.DeleteAsync(_selectedVisit); }); }

    private void SearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void SearchComboBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e) { }

    private static void SetupSearchComboBox(ComboBox comboBox, List<string> allItems)
    {
        comboBox.Tag = allItems;
        comboBox.ItemsSource = allItems;
        var tb = comboBox.Template?.FindName("PART_EditableTextBox", comboBox) as TextBox;
        if (tb != null) { tb.TextChanged += (s, ev) => {
            var text = tb.Text;
            comboBox.ItemsSource = allItems.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
            comboBox.IsDropDownOpen = true;
            tb.Text = text;
            tb.CaretIndex = text.Length;
        };}
    }
}
