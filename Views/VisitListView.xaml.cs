using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ClinicMS.Models;
using ClinicMS.ViewModels;
using ClinicMS.Services;

namespace ClinicMS.Views;

public partial class VisitListView : UserControl
{
    private MainViewModel? _vm;
    private Visit? _selectedVisit;
    private string _currentStatus = "Waiting";
    private bool _isDirty = false;
    private NotifyCollectionChangedEventHandler? _visitsChangedHandler;
    private DateTime? _selectedDateFilter;

    public VisitListView()
    {
        InitializeComponent();
        
        // MẶC ĐỊNH: Chọn lọc ngày hôm nay khi mở tab
        _selectedDateFilter = DateTime.Today;
        VisitDateFilter.SelectedDate = _selectedDateFilter;

        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;

        VisitListCard.SizeChanged += (s, e) =>
            VisitListCard.Clip = new RectangleGeometry(
                new Rect(0, 0, VisitListCard.ActualWidth, VisitListCard.ActualHeight), 16, 16);

        DetailCard.SizeChanged += (s, e) =>
            DetailCard.Clip = new RectangleGeometry(
                new Rect(0, 0, DetailCard.ActualWidth, DetailCard.ActualHeight), 16, 16);
    }

    private async void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm && _visitsChangedHandler != null)
            oldVm.Visits.CollectionChanged -= _visitsChangedHandler;

        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            _visitsChangedHandler = (s2, e2) => ApplyFilter();
            _vm.Visits.CollectionChanged += _visitsChangedHandler;
            
            ApplyFilter();
            if (_vm.Visits.Count == 0) await _vm.LoadVisitsAsync();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null && _visitsChangedHandler != null)
            _vm.Visits.CollectionChanged -= _visitsChangedHandler;
    }

    private int _searchRequestCount = 0;
    private string _search = "";
    private async void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        _search = ((TextBox)s).Text.Trim();
        int currentRequest = ++_searchRequestCount;
        await Task.Delay(250); 
        if (currentRequest == _searchRequestCount) ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(ApplyFilter); return; }
        
        var cvs = (CollectionViewSource)FindResource("VisitsViewSource");
        if (cvs?.View != null)
        {
            cvs.View.Refresh();
            
            // Đếm số lượng sau khi lọc
            int count = cvs.View.Cast<object>().Count();
            TxtTotalCount.Text = $"{count} lượt khám";

            // Cập nhật trạng thái Empty
            VisitEmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // LOGIC LỌC CỰC NHANH (Chạy cho từng item)
    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not Visit v) { e.Accepted = false; return; }

        // 1. Lọc theo ngày
        if (_selectedDateFilter.HasValue)
        {
            if (!VisitMatchesDate(v, _selectedDateFilter.Value)) { e.Accepted = false; return; }
        }

        // 2. Lọc theo từ khóa tìm kiếm (Dùng thuộc tính pre-cached)
        if (!string.IsNullOrWhiteSpace(_search))
        {
            var search = _search.ToLower();
            bool match = v.PatientName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                         v.DoctorName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                         (v.Diagnosis?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
            
            if (!match) { e.Accepted = false; return; }
        }

        e.Accepted = true;
    }

    private void VisitGroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VisitGroupList.SelectedItem is Visit v)
        {
            if (_isDirty && _selectedVisit != null && _selectedVisit.VisitId != v.VisitId)
            {
                var result = MessageBox.Show(
                    "Bạn có thay đổi chưa lưu. Bạn có chắc muốn chuyển sang bệnh nhân khác và mất các thay đổi này không?",
                    "Xác nhận chuyển", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                {
                    VisitGroupList.SelectionChanged -= VisitGroupList_SelectionChanged;
                    VisitGroupList.SelectedItem = _selectedVisit;
                    VisitGroupList.SelectionChanged += VisitGroupList_SelectionChanged;
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

        MedicineList.Children.Clear();
        if (_vm != null)
        {
            var details = await _vm.GetPrescriptionDetailsAsync(v.VisitId);
            if (details.Any())
            {
                MedicineEmpty.Visibility = Visibility.Collapsed;
                foreach (var d in details)
                {
                    MedicineList.Children.Add(MakeMedicineRow(d));
                }
            }
            else
            {
                MedicineEmpty.Visibility = Visibility.Visible;
            }
        }
    }

    private UIElement MakeMedicineRow(PrescriptionDetail d)
    {
        var grid = new Grid { Margin = new Thickness(16, 8, 16, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

        var nameTxt = new TextBlock { Text = d.Medicine?.MedicineName ?? $"Thuốc #{d.MedicineId}", FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        var qtyTxt = new TextBlock { Text = d.Quantity.ToString(), FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(qtyTxt, 1);
        var dosageTxt = new TextBlock { Text = d.Dosage, FontSize = 12, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(dosageTxt, 2);

        grid.Children.Add(nameTxt);
        grid.Children.Add(qtyTxt);
        grid.Children.Add(dosageTxt);

        return new Border { Child = grid, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6")), BorderThickness = new Thickness(0, 0, 0, 1) };
    }

    private void OnInputChanged(object sender, TextChangedEventArgs e) { _isDirty = true; }
    private void OnStatusChanged(object sender, RoutedEventArgs e) { _isDirty = true; }

    private void UpdateStatusButtons()
    {
        BtnWaiting.Background = NormalizeStatus(_currentStatus) == "Waiting" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")) : Brushes.White;
        BtnInProgress.Background = NormalizeStatus(_currentStatus) == "In Progress" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")) : Brushes.White;
        BtnCompleted.Background = NormalizeStatus(_currentStatus) == "Completed" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5")) : Brushes.White;
    }

    private void StatusWaiting_Click(object s, RoutedEventArgs e) { _currentStatus = "Waiting"; UpdateStatusButtons(); OnStatusChanged(s, e); }
    private void StatusInProgress_Click(object s, RoutedEventArgs e) { _currentStatus = "In Progress"; UpdateStatusButtons(); OnStatusChanged(s, e); }
    private void StatusCompleted_Click(object s, RoutedEventArgs e) { _currentStatus = "Completed"; UpdateStatusButtons(); OnStatusChanged(s, e); }

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
        
        var selectedId = _selectedVisit.VisitId;
        await _vm.UpdateVisitAsync(_selectedVisit);
        _selectedVisit = _vm.Visits.FirstOrDefault(x => x.VisitId == selectedId) ?? _selectedVisit;
        _isDirty = false;
        ApplyFilter();
    }

    private void CancelDetail_Click(object s, RoutedEventArgs e) { _selectedVisit = null; DetailForm.Visibility = Visibility.Collapsed; DetailEmpty.Visibility = Visibility.Visible; _isDirty = false; }
    private void AddMedicineDetail_Click(object sender, RoutedEventArgs e) 
    { 
        QuickAddMedicineArea.Visibility = Visibility.Visible;
        if (_vm != null)
        {
            var medicines = _vm.Medicines.Select(m => m.MedicineName ?? "").ToList();
            SetupSearchComboBox(QuickMedicineCombo, medicines);
        }
    }

    private void CancelQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        QuickAddMedicineArea.Visibility = Visibility.Collapsed;
        QuickMedicineCombo.Text = "";
        QuickMedicineQty.Clear();
        QuickMedicineDosage.Clear();
    }

    private async void ConfirmQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null || _selectedVisit == null) return;
        
        var medicineName = QuickMedicineCombo.Text.Trim();
        if (string.IsNullOrWhiteSpace(medicineName)) return;

        var med = _vm.Medicines.FirstOrDefault(m => m.MedicineName?.Equals(medicineName, StringComparison.OrdinalIgnoreCase) == true);
        if (med == null)
        {
            _vm.Toast.ShowError("Không tìm thấy thuốc này.");
            return;
        }
        
        double.TryParse(QuickMedicineQty.Text, out double qty);
        if (qty <= 0) qty = 1;

        var newItem = new PrescriptionDetail
        {
            VisitId = _selectedVisit.VisitId,
            MedicineId = med.MedicineId,
            Medicine = med,
            Quantity = qty,
            Dosage = QuickMedicineDosage.Text.Trim()
        };

        var currentDetails = await _vm.GetPrescriptionDetailsAsync(_selectedVisit.VisitId);
        
        var existing = currentDetails.FirstOrDefault(d => d.MedicineId == newItem.MedicineId);
        if (existing != null)
        {
            existing.Quantity += newItem.Quantity;
            existing.Dosage = newItem.Dosage;
        }
        else
        {
            currentDetails.Add(newItem);
        }

        await _vm.SavePrescriptionAsync(_selectedVisit.VisitId, currentDetails);
        
        CancelQuickAdd_Click(null!, null!);
        ShowDetail(_selectedVisit); 
        _vm.Toast.Show("Đã thêm thuốc vào đơn.");
    }

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

        tb.TextChanged -= EditableComboBox_TextChanged;

        try 
        {
            var filtered = string.IsNullOrWhiteSpace(text)
                ? sourceItems
                : sourceItems.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(25).ToList();

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
        if (cb.Template.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.CaretIndex = tb.Text.Length;
        }
    }

    private void SearchComboBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ComboBox cb) cb.IsDropDownOpen = false;
    }

    private async void Refresh_Click(object s, RoutedEventArgs e) { if (_vm != null) { await Task.WhenAll(_vm.LoadPatientsAsync(), _vm.LoadDoctorsAsync(), _vm.LoadVisitsAsync()); ApplyFilter(); } }
    private void VisitDateFilter_SelectedDateChanged(object s, SelectionChangedEventArgs e) { _selectedDateFilter = VisitDateFilter.SelectedDate?.Date; ApplyFilter(); }
    private void FilterToday_Click(object s, RoutedEventArgs e) { _selectedDateFilter = DateTime.Today; VisitDateFilter.SelectedDate = _selectedDateFilter; ApplyFilter(); }
    private void FilterAllDate_Click(object s, RoutedEventArgs e) { _selectedDateFilter = null; VisitDateFilter.SelectedDate = null; ApplyFilter(); }

    private void Add_Click(object s, RoutedEventArgs e) { if (_vm == null) return; var form = new VisitFormDialog(_vm); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadVisitsAsync(); }, () => form.HasData()); }
    private void Edit_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var form = new VisitFormDialog(_vm, _selectedVisit); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadVisitsAsync(); }, () => form.HasData()); }
    private void Delete_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var confirm = new ConfirmDialog("Xóa hồ sơ khám này?", "Thao tác không thể hoàn tác.", _vm); _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.DeleteVisitAsync(_selectedVisit); }); }
    
    private static string NormalizeStatus(string? status) { var s = status?.Trim(); if (string.IsNullOrWhiteSpace(s)) return "Waiting"; if (s.Equals("In Progress", StringComparison.OrdinalIgnoreCase) || s.Equals("InProgress", StringComparison.OrdinalIgnoreCase)) return "In Progress"; if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase)) return "Completed"; return "Waiting"; }
    private static string FormatVisitTime(string? visitTime) { if (TryParseVisitDateTime(visitTime, out var dt)) return dt.ToString("HH:mm"); return "--:--"; }
    private static string FormatVisitDate(string? visitTime) { if (TryParseVisitDateTime(visitTime, out var dt)) return dt.ToString("dd/MM/yyyy"); return ""; }
    private static bool TryParseVisitDateTime(string? value, out DateTime dt) { dt = default; if (string.IsNullOrWhiteSpace(value)) return false; var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy" }; if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return true; return DateTime.TryParse(value, out dt); }
    private static bool VisitMatchesDate(Visit v, DateTime searchDate) { if (!TryParseVisitDateTime(v.VisitTime, out var visitDateTime)) return false; return visitDateTime.Date == searchDate.Date; }
}
