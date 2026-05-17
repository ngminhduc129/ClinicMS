using System;
using System.Globalization;
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
        var dur = TimeSpan.FromSeconds(0.2);

        var translate = new TranslateTransform(0, 15);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = translate;

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        var animTranslate = new DoubleAnimation(15, 0, dur) { EasingFunction = ease };

        Timeline.SetDesiredFrameRate(animOpacity, 60);
        Timeline.SetDesiredFrameRate(animTranslate, 60);

        animOpacity.Freeze();
        animTranslate.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
        translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
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
        
        var result = await _vm.PatientVM.GetPatientWithContactAsync(p.PatientId);
        Contact? contact = result.contact;
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
            HeightCm = double.TryParse(HeightBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var h) ? h : null,
            WeightKg = double.TryParse(WeightBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var w) ? w : null,
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
        { 
            _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập họ tên bệnh nhân.", "Thiếu thông tin", true));
            return; 
        }

        string action = _isEditing ? "Cập nhật" : "Thêm mới";
        var confirm = new ConfirmDialog(
            $"{action} bệnh nhân?",
            $"Bạn có chắc chắn muốn {action.ToLower()} thông tin bệnh nhân \"{patient.FullName}\" không?",
            _vm);

        _vm.Modal.Show(confirm, async result => {
            if (result == true)
            {
                SaveBtn.IsEnabled = false;
                bool ok = _isEditing ? await _vm.PatientVM.UpdateAsync(patient) : await _vm.PatientVM.AddAsync(patient);
                if (ok) CloseForm(true);
                else 
                {
                    SaveBtn.IsEnabled = true;
                    _vm.Modal.Show(new MessageDialog(_vm, $"Không thể lưu thông tin bệnh nhân. Vui lòng kiểm tra lại (Họ tên có thể đã tồn tại hoặc lỗi kết nối).", "Lỗi lưu dữ liệu", true));
                }
            }
        });
    }

    private void Cancel_Click(object s, RoutedEventArgs e)
    {
        if (IsDirty())
        {
            _vm.Modal.Show(new ConfirmDialog("Hủy thay đổi?", "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?", _vm), (res) => {
                if (res == true) CloseForm();
            });
        }
        else CloseForm();
    }

    private void Close_Click(object s, RoutedEventArgs e)
    {
        if (IsDirty())
        {
            _vm.Modal.Show(new ConfirmDialog("Thoát form?", "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?", _vm), (res) => {
                if (res == true) CloseForm();
            });
        }
        else CloseForm();
    }

    private void Overlay_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.Source == e.OriginalSource || (e.OriginalSource is FrameworkElement fe && fe.Name != "DialogRoot" && FindParent<Border>(fe)?.Name != "DialogRoot"))
        {
            if (!IsDirty())
            {
                CloseForm();
                return;
            }

            _vm.Modal.Show(new ConfirmDialog("Thoát form?", "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?", _vm), (res) => {
                if (res == true) CloseForm();
            });
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }
}
