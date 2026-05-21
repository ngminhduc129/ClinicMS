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
    private NotifyCollectionChangedEventHandler? _visitsChangedHandler;

    // Collection phục vụ DataTemplate cho danh sách thuốc
    public ObservableCollection<PrescriptionDetail> SelectedPrescriptionDetails { get; } = new();

    // Collection phục vụ DataTemplate cho danh sách dịch vụ
    public ObservableCollection<ServiceOrder> SelectedServiceOrders { get; } = new();

    // Collection phục vụ hiển thị lịch sử khám của bệnh nhân hiện tại
    public ObservableCollection<Visit> PatientHistoryVisits { get; } = new();

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

    private void OnUnloaded(object s, RoutedEventArgs e)
    {
        if (_vm != null && _visitsChangedHandler != null)
        {
            _vm.VisitVM.Visits.CollectionChanged -= _visitsChangedHandler;
            _visitsChangedHandler = null;
        }
    }

    private async void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm && _visitsChangedHandler != null)
        {
            oldVm.VisitVM.Visits.CollectionChanged -= _visitsChangedHandler;
            _visitsChangedHandler = null;
        }

        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            
            // Cấu hình Grouping và Sorting cho danh sách
            var view = (ListCollectionView)CollectionViewSource.GetDefaultView(_vm.VisitVM.Visits);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("StatusGroup"));
            
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription("StatusOrder", System.ComponentModel.ListSortDirection.Ascending));
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription("VisitTime", System.ComponentModel.ListSortDirection.Descending));

            // Theo dõi thay đổi để ẩn/hiện empty state
            _visitsChangedHandler = (sender, args) => {
                VisitEmptyState.Visibility = _vm.VisitVM.Visits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            };
            _vm.VisitVM.Visits.CollectionChanged += _visitsChangedHandler;

            // Đồng bộ bộ lọc ngày hiện tại trên UI vào ViewModel
            if (VisitDateFilter.SelectedDate.HasValue)
            {
                _vm.VisitVM.VisitDateFilter = VisitDateFilter.SelectedDate.Value.ToString("yyyy-MM-dd");
            }

            if (_vm.VisitVM.Visits.Count == 0) await _vm.VisitVM.LoadAsync();
            VisitEmptyState.Visibility = _vm.VisitVM.Visits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
        else
        {
            _selectedVisit = null;
            _isDirty = false;
            DetailForm.Visibility = Visibility.Collapsed;
            DetailEmpty.Visibility = Visibility.Visible;
        }
    }

    private async void ShowDetail(Visit v)
    {
        _selectedVisit = v;
        _isDirty = false;
        DetailEmpty.Visibility = Visibility.Collapsed;
        DetailLoading.Visibility = Visibility.Collapsed;
        DetailForm.Visibility = Visibility.Visible;

        _currentStatus = NormalizeStatus(v.Status);
        UpdateStatusButtons();

        SelectedPrescriptionDetails.Clear();
        PatientHistoryVisits.Clear();

        if (_vm != null)
        {
            // 1. Tải danh sách thuốc của ca khám hiện tại
            var details = await _vm.VisitVM.GetPrescriptionDetailsAsync(v.VisitId);
            v.PrescriptionDetails = details; // Đồng bộ dữ liệu vào object gốc
            if (details.Any())
            {
                MedicineEmpty.Visibility = Visibility.Collapsed;
                foreach (var d in details) SelectedPrescriptionDetails.Add(d);
            }
            else
            {
                MedicineEmpty.Visibility = Visibility.Visible;
            }

            // 2. Nạp danh sách thuốc tự động hoàn thành cho In-line entry row
            if (_vm.MedicineVM.Medicines.Count == 0) await _vm.MedicineVM.LoadAsync();
            var medicines = _vm.MedicineVM.Medicines.Select(m => m.MedicineName ?? "").OrderBy(n => n).ToList();
            QuickMedicineCombo.ApplyTemplate();
            SetupSearchComboBox(QuickMedicineCombo, medicines);

            // 3. Tải danh sách dịch vụ của ca khám hiện tại
            SelectedServiceOrders.Clear();
            var orders = await _vm.VisitVM.GetServiceOrdersAsync(v.VisitId);
            v.ServiceOrders = orders; // Đồng bộ dữ liệu vào object gốc
            if (orders.Any())
            {
                ServiceEmpty.Visibility = Visibility.Collapsed;
                foreach (var o in orders) SelectedServiceOrders.Add(o);
            }
            else
            {
                ServiceEmpty.Visibility = Visibility.Visible;
            }

            // 4. Nạp danh sách dịch vụ tự động hoàn thành cho In-line entry row
            if (_vm.ServiceVM.Services.Count == 0) await _vm.ServiceVM.LoadAsync();
            var services = _vm.ServiceVM.Services.Select(s => s.ServiceName ?? "").OrderBy(n => n).ToList();
            QuickServiceCombo.ApplyTemplate();
            SetupSearchComboBox(QuickServiceCombo, services);

            // 5. Tải lịch sử bệnh án của bệnh nhân
            await LoadPatientHistoryAsync(v);
        }
    }

    private void Input_TextChanged(object sender, TextChangedEventArgs e)
    {
        _isDirty = true;
    }

    private void UpdateStatusButtons()
    {
        // Reset colors and borders
        BtnWaiting.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
        BtnWaiting.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));

        BtnInProgress.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
        BtnInProgress.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE"));

        BtnCompleted.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
        BtnCompleted.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"));

        // Highlight active with a thicker or distinct border
        if (_currentStatus == "Waiting") BtnWaiting.BorderBrush = Brushes.Orange;
        else if (_currentStatus == "In Progress") BtnInProgress.BorderBrush = Brushes.Blue;
        else if (_currentStatus == "Completed") BtnCompleted.BorderBrush = Brushes.Green;
    }

    private void StatusWaiting_Click(object s, RoutedEventArgs e) { _currentStatus = "Waiting"; if(_selectedVisit != null) _selectedVisit.Status = "Waiting"; UpdateStatusButtons(); _isDirty = true; }
    private void StatusInProgress_Click(object s, RoutedEventArgs e) { _currentStatus = "In Progress"; if(_selectedVisit != null) _selectedVisit.Status = "In Progress"; UpdateStatusButtons(); _isDirty = true; }
    private void StatusCompleted_Click(object s, RoutedEventArgs e) { _currentStatus = "Completed"; if(_selectedVisit != null) _selectedVisit.Status = "Completed"; UpdateStatusButtons(); _isDirty = true; }

    private async void RemoveMedicineDetail_Click(object sender, RoutedEventArgs e)
    {
        try 
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
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi xóa thuốc: {ex.Message}");
        }
    }

    private const int MaxSuggestionItems = 10;

    private async Task LoadPatientHistoryAsync(Visit? v = null)
    {
        if (_vm == null) return;
        v ??= _selectedVisit;
        if (v == null) return;

        try
        {
            PatientHistoryVisits.Clear();
            var history = await _vm.PatientVM.GetPatientVisitsAsync(v.PatientId);
            var completedVisits = history.Where(x => x.Status == "Completed").ToList();
            foreach (var visit in completedVisits)
            {
                PatientHistoryVisits.Add(visit);
            }
        }
        catch (Exception ex)
        {
            _vm.Toast.ShowError($"Lỗi khi tải lịch sử khám: {ex.Message}");
        }
    }

    private void ClearQuickAddFields()
    {
        QuickMedicineCombo.Text = "";
        QuickMedicineQty.Text = "1";
        QuickMedicineDosage.Text = "";
        
        QuickMedicineCombo.Focus();
        if (QuickMedicineCombo.Template?.FindName("PART_EditableTextBox", QuickMedicineCombo) is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    private void QuickMedicineDosage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            ConfirmQuickAdd_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void ConfirmQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        try 
        {
            if (_vm == null || _selectedVisit == null) return;
            var medicineName = QuickMedicineCombo.Text.Trim();
            if (string.IsNullOrWhiteSpace(medicineName)) return;
            var med = _vm.MedicineVM.Medicines.FirstOrDefault(m => m.MedicineName?.Equals(medicineName, StringComparison.OrdinalIgnoreCase) == true);
            if (med == null) { _vm.Toast.ShowError("Không tìm thấy thuốc này."); return; }
            
            // KIỂM TRA TỒN KHO
            if (med.AvailableQuantity <= 0)
            {
                _vm.Toast.ShowError($"Thuốc '{med.MedicineName}' đã hết hoặc đã bị giữ chỗ hết.");
                return;
            }

            double.TryParse(QuickMedicineQty.Text, out double qty);
            if (qty <= 0) qty = 1;

            if (qty > med.AvailableQuantity)
            {
                _vm.Toast.ShowError($"Số lượng kê ({qty}) vượt quá số lượng khả dụng ({med.AvailableQuantity}).");
                return;
            }
            var newItem = new PrescriptionDetail { VisitId = _selectedVisit.VisitId, MedicineId = med.MedicineId, Medicine = med, Quantity = qty, Dosage = QuickMedicineDosage.Text.Trim() };
            var currentDetails = await _vm.VisitVM.GetPrescriptionDetailsAsync(_selectedVisit.VisitId);
            var existing = currentDetails.FirstOrDefault(d => d.MedicineId == newItem.MedicineId);
            if (existing != null) { existing.Quantity += newItem.Quantity; existing.Dosage = newItem.Dosage; }
            else currentDetails.Add(newItem);
            await _vm.VisitVM.SavePrescriptionAsync(_selectedVisit.VisitId, currentDetails);
            ShowDetail(_selectedVisit); 
            _vm.Toast.Show("Đã thêm thuốc vào đơn.");
            ClearQuickAddFields();
        }
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi thêm thuốc: {ex.Message}");
        }
    }

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        try 
        {
            if (_vm == null || _selectedVisit == null) return;
            
            // Với Data Binding, các thuộc tính của _selectedVisit đã được cập nhật tự động từ UI
            // Đảm bảo đơn thuốc và dịch vụ chỉ định được đồng bộ từ UI vào object trước khi lưu
            _selectedVisit.PrescriptionDetails = SelectedPrescriptionDetails.ToList();
            _selectedVisit.ServiceOrders = SelectedServiceOrders.ToList();

            int selectedId = _selectedVisit.VisitId;
            // Unsubscribe trước vì UpdateAsync → LoadAsync → Visits.Clear() sẽ deselect ListBox
            VisitGroupList.SelectionChanged -= VisitGroupList_SelectionChanged;
            try
            {
                bool success = await _vm.VisitVM.UpdateAsync(_selectedVisit);
                if (success)
                {
                    _isDirty = false;
                    
                    var updatedVisit = _vm.VisitVM.Visits.FirstOrDefault(x => x.VisitId == selectedId);
                    if (updatedVisit != null)
                    {
                        VisitGroupList.SelectedItem = updatedVisit;
                        _selectedVisit = updatedVisit;
                    }
                    
                    _vm.Toast.Show("Đã lưu thay đổi.");
                }
                await LoadPatientHistoryAsync();
            }
            finally
            {
                VisitGroupList.SelectionChanged += VisitGroupList_SelectionChanged;
            }
        }
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi lưu: {ex.Message}");
        }
    }

    private void CancelDetail_Click(object s, RoutedEventArgs e) { _selectedVisit = null; DetailForm.Visibility = Visibility.Collapsed; DetailEmpty.Visibility = Visibility.Visible; _isDirty = false; }
    private string FormatVisitDate(string? time) => DateTime.TryParse(time, out var dt) ? dt.ToString("dd/MM/yyyy HH:mm") : (time ?? "--");
    private string NormalizeStatus(string? status) => string.IsNullOrEmpty(status) ? "Waiting" : status;

    private async void Refresh_Click(object s, RoutedEventArgs e) 
    { 
        try 
        {
            if (_vm != null) await Task.WhenAll(_vm.PatientVM.LoadAsync(), _vm.DoctorVM.LoadAsync(), _vm.VisitVM.LoadAsync(), _vm.MedicineVM.LoadAsync()); 
        }
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi làm mới: {ex.Message}");
        }
    }

    private async void VisitDateFilter_SelectedDateChanged(object s, SelectionChangedEventArgs e)
    {
        try 
        {
            if (_vm == null) return;
            await _vm.VisitVM.SearchByDateAsync(VisitDateFilter.SelectedDate?.ToString("yyyy-MM-dd"));
        }
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi lọc ngày: {ex.Message}");
        }
    }
    private void FilterToday_Click(object s, RoutedEventArgs e) { VisitDateFilter.SelectedDate = DateTime.Today; }
    private void FilterAllDate_Click(object s, RoutedEventArgs e) { VisitDateFilter.SelectedDate = null; }

    private void Add_Click(object s, RoutedEventArgs e) { if (_vm == null) return; var form = new VisitFormDialog(_vm); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.VisitVM.LoadAsync(); }, () => !form.HasData()); }
    private void Edit_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || _selectedVisit == null) return;
        int selectedId = _selectedVisit.VisitId;
        var form = new VisitFormDialog(_vm, _selectedVisit);
        _vm.Modal.Show(form, async result =>
        {
            if (result == true)
            {
                await _vm.VisitVM.LoadAsync();
                var updatedVisit = _vm.VisitVM.Visits.FirstOrDefault(x => x.VisitId == selectedId);
                if (updatedVisit != null)
                {
                    VisitGroupList.SelectedItem = updatedVisit;
                }
            }
        }, () => !form.HasData());
    }
    private void Delete_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var confirm = new ConfirmDialog("Xóa hồ sơ khám này?", "Thao tác không thể hoàn tác.", _vm); _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.VisitVM.DeleteAsync(_selectedVisit); }); }

    private void SearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) 
    {
        if (sender is ComboBox cb && cb.SelectedItem is string selected)
        {
            cb.Text = selected;
            cb.IsDropDownOpen = false;
        }
    }

    private void SearchComboBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e) 
    {
        if (sender is ComboBox cb) cb.IsDropDownOpen = false;
    }

    private static void SetupSearchComboBox(ComboBox comboBox, List<string> allItems)
    {
        comboBox.Tag = allItems;
        comboBox.ItemsSource = allItems;
        comboBox.IsTextSearchEnabled = false;

        var tb = comboBox.Template?.FindName("PART_EditableTextBox", comboBox) as TextBox;
        if (tb != null)
        {
            tb.TextChanged -= QuickMedicine_TextChanged;
            tb.TextChanged += QuickMedicine_TextChanged;
        }
        else
        {
            comboBox.Loaded += (s, e) =>
            {
                if (comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is TextBox tb2)
                {
                    tb2.TextChanged -= QuickMedicine_TextChanged;
                    tb2.TextChanged += QuickMedicine_TextChanged;
                }
            };
        }
    }

    private static void QuickMedicine_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;
        if (cb.Tag is not List<string> allItems) return;

        var text = tb.Text;
        var caretIndex = tb.CaretIndex;

        tb.TextChanged -= QuickMedicine_TextChanged;
        try
        {
            var filtered = string.IsNullOrWhiteSpace(text) 
                ? allItems.Take(MaxSuggestionItems).ToList()
                : allItems.Where(n => n.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).Take(MaxSuggestionItems).ToList();
            
            cb.ItemsSource = filtered;
            
            // Khôi phục text vì ItemsSource thay đổi làm mất trạng thái
            tb.Text = text;
            cb.Text = text;
            tb.CaretIndex = caretIndex;

            // Ép dropdown mở xuống dưới khi có kết quả
            cb.IsDropDownOpen = filtered.Any() && tb.IsFocused;
        }
        finally
        {
            tb.TextChanged += QuickMedicine_TextChanged;
        }
    }

    private async void ConfirmQuickAddService_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm == null || _selectedVisit == null) return;
            var serviceName = QuickServiceCombo.Text.Trim();
            if (string.IsNullOrWhiteSpace(serviceName)) return;

            var svc = _vm.ServiceVM.Services.FirstOrDefault(s => s.ServiceName?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true);
            if (svc == null) { _vm.Toast.ShowError("Không tìm thấy dịch vụ này."); return; }

            // Kiểm tra trùng
            if (SelectedServiceOrders.Any(o => o.ServiceId == svc.ServiceId))
            {
                _vm.Toast.ShowError("Dịch vụ này đã được chỉ định.");
                return;
            }

            var newOrder = new ServiceOrder 
            { 
                VisitId = _selectedVisit.VisitId, 
                ServiceId = svc.ServiceId, 
                Service = svc, 
                Result = QuickServiceResult.Text.Trim() 
            };
            
            // Lấy danh sách dịch vụ hiện tại từ db để giữ tính đồng bộ
            var currentOrders = await _vm.VisitVM.GetServiceOrdersAsync(_selectedVisit.VisitId);
            currentOrders.Add(newOrder);
            
            await _vm.VisitVM.SaveServiceOrdersAsync(_selectedVisit.VisitId, currentOrders);
            
            ShowDetail(_selectedVisit);
            _vm.Toast.Show("Đã chỉ định dịch vụ.");
            
            QuickServiceCombo.Text = "";
            QuickServiceResult.Text = "";
            QuickServiceCombo.Focus();
        }
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi chỉ định dịch vụ: {ex.Message}");
        }
    }

    private async void RemoveServiceOrder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_vm == null || _selectedVisit == null || sender is not Button btn || btn.Tag is not ServiceOrder order) return;
            var currentOrders = await _vm.VisitVM.GetServiceOrdersAsync(_selectedVisit.VisitId);
            var toRemove = currentOrders.FirstOrDefault(o => o.ServiceId == order.ServiceId);
            if (toRemove != null)
            {
                currentOrders.Remove(toRemove);
                await _vm.VisitVM.SaveServiceOrdersAsync(_selectedVisit.VisitId, currentOrders);
                ShowDetail(_selectedVisit);
                _vm.Toast.Show("Đã xóa dịch vụ chỉ định.");
            }
        }
        catch (Exception ex)
        {
            _vm?.Toast.ShowError($"Lỗi khi xóa dịch vụ chỉ định: {ex.Message}");
        }
    }

    private void QuickServiceResult_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            ConfirmQuickAddService_Click(sender, e);
            e.Handled = true;
        }
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (PresentationSource.FromDependencyObject(e.OriginalSource as DependencyObject) != PresentationSource.FromDependencyObject(this))
                return;

            QuickMedicineCombo.IsDropDownOpen = false;
            QuickServiceCombo.IsDropDownOpen = false;

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
