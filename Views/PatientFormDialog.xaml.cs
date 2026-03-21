using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class PatientFormDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Patient? _editing;
    private bool _isEditing => _editing != null;

    private static readonly SolidColorBrush _borderFocused = new(Color.FromRgb(22, 163, 74));
    private static readonly SolidColorBrush _borderNormal = new(Color.FromRgb(229, 231, 235));
    private static readonly SolidColorBrush _borderHover = new(Color.FromRgb(134, 239, 172));

    public PatientFormDialog(MainViewModel vm, Patient? patient = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = patient;
        Opacity = 0;

        if (_isEditing && patient != null)
        {
            TitleText.Text = "Sửa Thông Tin Bệnh Nhân";
            SubtitleText.Text = "Cập nhật thông tin bệnh nhân";
            Loaded += (_, _) =>
            {
                if (SaveBtn.Template?.FindName("SaveLabel", SaveBtn) is TextBlock lbl)
                    lbl.Text = "Cập Nhật";
            };
            _ = LoadDataAsync(patient);
        }

        Loaded += (_, _) => AnimateOpen();
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

        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, dur) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(40, 0, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
    }

    private void CloseForm(bool? result = null)
    {
        if (DataContext is MainViewModel vm) vm.Modal.Close(result);
        else _vm.Modal.Close(result);
    }

    // ───────────────────────────── DatePicker focus ─────────────────────────────
    private void DobPicker_GotFocus(object sender, RoutedEventArgs e)
    {
        DobBorder.BorderBrush = _borderFocused;
        DobBorder.Effect = new DropShadowEffect { Color = Color.FromRgb(34, 197, 94), Opacity = 0.20, BlurRadius = 10, ShadowDepth = 0 };
    }

    private void DobPicker_LostFocus(object sender, RoutedEventArgs e)
    { DobBorder.BorderBrush = _borderNormal; DobBorder.Effect = null; }

    private void DobBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    { if (!DobPicker.IsFocused) DobBorder.BorderBrush = _borderHover; }

    private void DobBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    { if (!DobPicker.IsFocused) DobBorder.BorderBrush = _borderNormal; }

    // ───────────────────────────── Data ─────────────────────────────
    private async Task LoadDataAsync(Patient p)
    {
        NameBox.Text = p.FullName ?? ""; NotesBox.Text = p.Note ?? "";
        HeightBox.Text = p.HeightCm?.ToString() ?? ""; WeightBox.Text = p.WeightKg?.ToString() ?? "";
        if (DateTime.TryParse(p.DateOfBirth, out var d)) DobPicker.SelectedDate = d;
        GenderNam.IsChecked = p.Gender == "Nam";
        GenderNu.IsChecked = p.Gender == "Nữ";
        GenderOther.IsChecked = p.Gender != "Nam" && p.Gender != "Nữ" && !string.IsNullOrEmpty(p.Gender);
        var bt = BloodTypeBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == p.BloodType);
        if (bt != null) BloodTypeBox.SelectedItem = bt;
        var (_, contact) = await _vm.GetPatientWithContactAsync(p.PatientId);
        if (contact != null) { PhoneBox.Text = contact.Phone ?? ""; EmailBox.Text = contact.Email ?? ""; AddressBox.Text = contact.Address ?? ""; }
    }

    private Patient BuildPatient()
    {
        string gender = GenderNam.IsChecked == true ? "Nam" : GenderNu.IsChecked == true ? "Nữ" : "Khác";
        return new Patient
        {
            PatientId = _editing?.PatientId ?? 0,
            FullName = NameBox.Text.Trim(),
            DateOfBirth = DobPicker.SelectedDate?.ToString("yyyy-MM-dd"),
            Gender = gender,
            BloodType = (BloodTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
            HeightCm = double.TryParse(HeightBox.Text.Trim(), out var h) ? h : null,
            WeightKg = double.TryParse(WeightBox.Text.Trim(), out var w) ? w : null,
            Note = NotesBox.Text.Trim(),
            Phone = PhoneBox.Text.Trim(),
            Email = EmailBox.Text.Trim(),
            Address = AddressBox.Text.Trim()
        };
    }

    public bool IsDirty()
    {
        if (!_isEditing)
        {
            return !string.IsNullOrWhiteSpace(NameBox.Text) || !string.IsNullOrWhiteSpace(PhoneBox.Text) ||
                   !string.IsNullOrWhiteSpace(EmailBox.Text) || !string.IsNullOrWhiteSpace(AddressBox.Text) ||
                   !string.IsNullOrWhiteSpace(NotesBox.Text) || !string.IsNullOrWhiteSpace(HeightBox.Text) ||
                   !string.IsNullOrWhiteSpace(WeightBox.Text) || DobPicker.SelectedDate.HasValue;
        }

        if (_editing == null) return false;

        // So sánh với dữ liệu cũ khi Edit
        bool nameChanged = (NameBox.Text.Trim() != (_editing.FullName ?? ""));
        bool notesChanged = (NotesBox.Text.Trim() != (_editing.Note ?? ""));
        bool heightChanged = (HeightBox.Text.Trim() != (_editing.HeightCm?.ToString() ?? ""));
        bool weightChanged = (WeightBox.Text.Trim() != (_editing.WeightKg?.ToString() ?? ""));
        
        string oldDob = _editing.DateOfBirth ?? "";
        string newDob = DobPicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
        bool dobChanged = oldDob != newDob;

        string oldGender = _editing.Gender ?? "Nam";
        string newGender = GenderNam.IsChecked == true ? "Nam" : GenderNu.IsChecked == true ? "Nữ" : "Khác";
        bool genderChanged = oldGender != newGender;

        string oldBlood = _editing.BloodType ?? "";
        string newBlood = (BloodTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        bool bloodChanged = oldBlood != newBlood;

        bool phoneChanged = (PhoneBox.Text.Trim() != (_editing.Phone ?? ""));
        bool emailChanged = (EmailBox.Text.Trim() != (_editing.Email ?? ""));
        bool addressChanged = (AddressBox.Text.Trim() != (_editing.Address ?? ""));

        return nameChanged || notesChanged || heightChanged || weightChanged || 
               dobChanged || genderChanged || bloodChanged ||
               phoneChanged || emailChanged || addressChanged;
    }

    public bool HasData() => IsDirty();

    // ───────────────────────────── Button events ─────────────────────────────
    private async void Save_Click(object s, RoutedEventArgs e)
    {
        var patient = BuildPatient();
        if (string.IsNullOrWhiteSpace(patient.FullName))
        { MessageBox.Show("Vui lòng nhập họ tên bệnh nhân.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        SaveBtn.IsEnabled = false;
        bool ok = _isEditing ? await _vm.UpdatePatientAsync(patient) : await _vm.AddPatientAsync(patient);
        if (ok) CloseForm(true);
        else SaveBtn.IsEnabled = true;
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

    private void Overlay_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not Grid g || g.Name != "Overlay") return;
        
        // Nếu không có thay đổi gì thì đóng luôn, không cần hỏi
        if (!IsDirty())
        {
            CloseForm();
            return;
        }

        // Nếu có thay đổi, có thể hỏi hoặc chỉ đơn giản là không đóng (ở đây chọn là hỏi để an toàn)
        var result = MessageBox.Show(
            "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?",
            "Thoát form?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) CloseForm();
    }
}