using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class VisitListView : UserControl
{
    private MainViewModel? _vm;
    private Visit? _selectedVisit;
    private string _currentStatus = "Waiting";
    private NotifyCollectionChangedEventHandler? _visitsChangedHandler;
    private DateTime? _selectedDateFilter;

    public ObservableCollection<Visit> FilteredVisits { get; } = new();

    public VisitListView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;

        VisitListCard.SizeChanged += (s, e) =>
            VisitListCard.Clip = new RectangleGeometry(
                new Rect(0, 0, VisitListCard.ActualWidth, VisitListCard.ActualHeight), 16, 16);

        DetailCard.SizeChanged += (s, e) =>
            DetailCard.Clip = new RectangleGeometry(
                new Rect(0, 0, DetailCard.ActualWidth, DetailCard.ActualHeight), 16, 16);
    }

    // ══════════════════════════════════════════
    // DATA LOAD
    // ══════════════════════════════════════════

    private async void OnDataContextChanged(object s, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm && _visitsChangedHandler != null)
        {
            oldVm.Visits.CollectionChanged -= _visitsChangedHandler;
        }

        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            _visitsChangedHandler = (s2, e2) => ApplyFilter();
            _vm.Visits.CollectionChanged += _visitsChangedHandler;

            // Đảm bảo UI cập nhật nếu data đã có sẵn
            ApplyFilter();
            
            // Tải thêm nếu cần
            if (_vm.Visits.Count == 0)
            {
                await _vm.LoadVisitsAsync();
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null && _visitsChangedHandler != null)
        {
            _vm.Visits.CollectionChanged -= _visitsChangedHandler;
        }
    }

    private string _search = "";

    private void Search_TextChanged(object s, TextChangedEventArgs e)
    {
        _search = ((TextBox)s).Text.Trim();
        ApplyFilter();
    }

    private string GetPatientName(Visit v)
        => v.Patient?.FullName
           ?? _vm?.Patients.FirstOrDefault(p => p.PatientId == v.PatientId)?.FullName
           ?? "";

    private string GetDoctorName(Visit v)
        => v.Doctor?.FullName
           ?? _vm?.Doctors.FirstOrDefault(d => d.DoctorId == v.DoctorId)?.FullName
           ?? "";

    private void ApplyFilter()
    {
        if (_vm == null) return;

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ApplyFilter);
            return;
        }

        var q = _vm.Visits.Where(v =>
            (string.IsNullOrWhiteSpace(_search) ||
             GetPatientName(v).Contains(_search, StringComparison.OrdinalIgnoreCase) ||
             GetDoctorName(v).Contains(_search, StringComparison.OrdinalIgnoreCase) ||
             (v.Diagnosis?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false))
            && (!_selectedDateFilter.HasValue || VisitMatchesDate(v, _selectedDateFilter.Value)));

        FilteredVisits.Clear();
        foreach (var v in q) FilteredVisits.Add(v);

        // Đồng bộ selection theo VisitId sau mỗi lần lọc/refresh
        if (_selectedVisit != null)
        {
            var newSelection = FilteredVisits.FirstOrDefault(x => x.VisitId == _selectedVisit.VisitId);
            _selectedVisit = newSelection;
            if (_selectedVisit == null)
            {
                DetailForm.Visibility = Visibility.Collapsed;
                DetailLoading.Visibility = Visibility.Collapsed;
                DetailEmpty.Visibility = Visibility.Visible;
            }
            else
            {
                ShowDetail(_selectedVisit);
            }
        }

        RebuildVisitList();
    }

    // ══════════════════════════════════════════
    // VISIT LIST (LEFT PANEL)
    // ══════════════════════════════════════════

    private void RebuildVisitList()
    {
        VisitGroupList.Items.Clear();

        var waiting = FilteredVisits.Where(v => NormalizeStatus(v.Status) == "Waiting").ToList();
        var inProgress = FilteredVisits.Where(v => NormalizeStatus(v.Status) == "InProgress").ToList();
        var completed = FilteredVisits.Where(v => NormalizeStatus(v.Status) == "Completed").ToList();

        if (waiting.Count > 0)
        {
            VisitGroupList.Items.Add(MakeSectionHeader($"CHỜ KHÁM ({waiting.Count})"));
            foreach (var v in waiting) VisitGroupList.Items.Add(MakeVisitItem(v, "#F59E0B"));
        }
        if (inProgress.Count > 0)
        {
            VisitGroupList.Items.Add(MakeSectionHeader($"ĐANG KHÁM ({inProgress.Count})"));
            foreach (var v in inProgress) VisitGroupList.Items.Add(MakeVisitItem(v, "#3B82F6"));
        }
        if (completed.Count > 0)
        {
            VisitGroupList.Items.Add(MakeSectionHeader($"ĐÃ KHÁM ({completed.Count})"));
            foreach (var v in completed) VisitGroupList.Items.Add(MakeVisitItem(v, "#16A34A"));
        }

        VisitEmptyState.Visibility = FilteredVisits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Border MakeSectionHeader(string title) => new()
    {
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB")),
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6")),
        BorderThickness = new Thickness(0, 0, 0, 1),
        Padding = new Thickness(16, 10, 16, 10),
        Child = new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
            FontFamily = new FontFamily("Segoe UI")
        }
    };

    private Border MakeVisitItem(Visit v, string dotColor)
    {
        bool isSelected = _selectedVisit?.VisitId == v.VisitId;
        var patientName = GetPatientName(v);
        var visitType = v.VisitType;

        var dot = new Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#16A34A" : dotColor))
        };

        var timeText = FormatVisitTime(v.VisitTime);

        var time = new TextBlock
        {
            Text = timeText, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
            FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(8, 0, 0, 0)
        };

        var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Width = 75 };
        timeRow.Children.Add(dot); timeRow.Children.Add(time);

        var name = new TextBlock
        {
            Text = patientName, FontSize = 13, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#111827" : "#1F2937")),
            FontFamily = new FontFamily("Segoe UI"), TextTrimming = TextTrimming.CharacterEllipsis
        };

        var typeLabel = new TextBlock
        {
            Text = visitType == "Walk-in" ? "Khám tổng quát" : (visitType ?? "Khám bệnh"),
            FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
            FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 2, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var info = new StackPanel { Orientation = Orientation.Vertical };
        info.Children.Add(name); info.Children.Add(typeLabel);

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(timeRow); row.Children.Add(info);

        var item = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#F0FDF4" : "#FFFFFF")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent")),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 12, 16, 12),
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = row, Tag = v
        };

        item.MouseLeftButtonUp += VisitItem_Click;
        item.MouseEnter += (s, e) => { if (_selectedVisit?.VisitId != v.VisitId) item.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB")); };
        item.MouseLeave += (s, e) => { if (_selectedVisit?.VisitId != v.VisitId) item.Background = new SolidColorBrush(Colors.White); };

        return item;
    }

    private void VisitItem_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (s is Border b && b.Tag is Visit v)
        {
            _selectedVisit = v;
            RebuildVisitList();
            ShowDetail(v);
        }
    }

    // ══════════════════════════════════════════
    // DETAIL PANEL (RIGHT)
    // ══════════════════════════════════════════

    private void ShowDetail(Visit v)
    {
        DetailEmpty.Visibility = Visibility.Collapsed;
        DetailLoading.Visibility = Visibility.Collapsed;
        DetailForm.Visibility = Visibility.Visible;

        var patientName = GetPatientName(v);
        var doctorName = GetDoctorName(v);

        DetailAvatar.Text = !string.IsNullOrEmpty(patientName) ? patientName[0].ToString().ToUpper() : "?";
        DetailPatientName.Text = patientName;
        DetailPatientGender.Text = $"Bác sĩ: {doctorName}";
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
        MedicineEmpty.Visibility = Visibility.Visible;
    }

    private void UpdateStatusButtons()
    {
        var activeGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
        var activeAmber = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
        var activeBlue = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
        var borderGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"));
        var borderAmber = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
        var borderBlue = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE"));
        var inactive = new SolidColorBrush(Colors.White);
        var inactiveBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));

        BtnWaiting.Background = _currentStatus == "Waiting" ? activeAmber : inactive;
        BtnWaiting.BorderBrush = _currentStatus == "Waiting" ? borderAmber : inactiveBorder;
        
        BtnInProgress.Background = (_currentStatus == "InProgress" || _currentStatus == "In Progress") ? activeBlue : inactive;
        BtnInProgress.BorderBrush = (_currentStatus == "InProgress" || _currentStatus == "In Progress") ? borderBlue : inactiveBorder;
        
        BtnCompleted.Background = _currentStatus == "Completed" ? activeGreen : inactive;
        BtnCompleted.BorderBrush = _currentStatus == "Completed" ? borderGreen : inactiveBorder;
    }

    private void StatusWaiting_Click(object s, RoutedEventArgs e) { _currentStatus = "Waiting"; UpdateStatusButtons(); }
    private void StatusInProgress_Click(object s, RoutedEventArgs e) { _currentStatus = "InProgress"; UpdateStatusButtons(); }
    private void StatusCompleted_Click(object s, RoutedEventArgs e) { _currentStatus = "Completed"; UpdateStatusButtons(); }

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null || _selectedVisit == null) return;
        _selectedVisit.Status = _currentStatus == "InProgress" ? "In Progress" : _currentStatus;
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
        ApplyFilter();
    }

    private void CancelDetail_Click(object s, RoutedEventArgs e)
    {
        _selectedVisit = null;
        DetailForm.Visibility = Visibility.Collapsed;
        DetailLoading.Visibility = Visibility.Collapsed;
        DetailEmpty.Visibility = Visibility.Visible;
        RebuildVisitList();
    }

    private async void Refresh_Click(object s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        await Task.WhenAll(_vm.LoadPatientsAsync(), _vm.LoadDoctorsAsync(), _vm.LoadVisitsAsync());
        ApplyFilter();
    }

    private void VisitDateFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedDateFilter = VisitDateFilter.SelectedDate?.Date;
        ApplyFilter();
    }

    private void FilterToday_Click(object sender, RoutedEventArgs e)
    {
        _selectedDateFilter = DateTime.Today;
        VisitDateFilter.SelectedDate = _selectedDateFilter;
        ApplyFilter();
    }

    private void FilterAllDate_Click(object sender, RoutedEventArgs e)
    {
        _selectedDateFilter = null;
        VisitDateFilter.SelectedDate = null;
        ApplyFilter();
    }

    private void AddMedicineDetail_Click(object s, RoutedEventArgs e)
    {
        // Khi nhấn Thêm thuốc ở bảng chi tiết, ta mở Form Edit chuyên sâu
        // vì Form đó đã có sẵn giao diện chọn thuốc chuyên nghiệp.
        Edit_Click(s, e);
    }

    private void Add_Click(object s, RoutedEventArgs e) { if (_vm == null) return; var form = new VisitFormDialog(_vm); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadVisitsAsync(); }, () => form.HasData()); }
    private void Edit_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var form = new VisitFormDialog(_vm, _selectedVisit); _vm.Modal.Show(form, result => { if (result == true) _ = _vm.LoadVisitsAsync(); }, () => form.HasData()); }
    private void Delete_Click(object s, RoutedEventArgs e) { if (_vm == null || _selectedVisit == null) return; var confirm = new ConfirmDialog("Xóa hồ sơ khám này?", "Thao tác không thể hoàn tác.", _vm); _vm.Modal.Show(confirm, async result => { if (result == true) await _vm.DeleteVisitAsync(_selectedVisit); }); }
    private void VisitGrid_SelectionChanged(object s, SelectionChangedEventArgs e) { }

    private static string NormalizeStatus(string? status)
    {
        var s = status?.Trim();
        if (string.IsNullOrWhiteSpace(s)) return "Waiting";
        if (s.Equals("In Progress", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
            return "InProgress";
        if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            return "Completed";
        return "Waiting";
    }

    private static string FormatVisitTime(string? visitTime)
    {
        if (TryParseVisitDateTime(visitTime, out var dt))
            return dt.ToString("HH:mm");
        return "--:--";
    }

    private static string FormatVisitDate(string? visitTime)
    {
        if (TryParseVisitDateTime(visitTime, out var dt))
            return dt.ToString("dd/MM/yyyy");
        return "";
    }

    private static bool TryParseVisitDateTime(string? value, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return true;
        return DateTime.TryParse(value, out dt);
    }

    private static bool VisitMatchesDate(Visit v, DateTime searchDate)
    {
        if (!TryParseVisitDateTime(v.VisitTime, out var visitDateTime)) return false;
        return visitDateTime.Date == searchDate.Date;
    }
}
