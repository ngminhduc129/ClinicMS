using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class DoctorFormDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Doctor? _editing;
    private bool _isEditing => _editing != null;

    public DoctorFormDialog(MainViewModel vm, Doctor? doctor = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = doctor;
        
        Opacity = 0;

        if (_isEditing && doctor != null)
        {
            TitleText.Text = "Sửa Thông Tin Bác Sĩ";
            SubtitleText.Text = "Cập nhật thông tin bác sĩ";
            Loaded += (_, _) =>
            {
                if (SaveBtn.Template?.FindName("SaveLabel", SaveBtn) is TextBlock lbl)
                    lbl.Text = "Cập Nhật";
            };
            LoadData(doctor);
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

    private void LoadData(Doctor d)
    {
        NameBox.Text = d.FullName ?? "";
        SpecialtyBox.Text = d.Specialty ?? "";
        PhoneBox.Text = d.Phone ?? "";
        EmailBox.Text = d.Email ?? "";
        NoteBox.Text = d.Note ?? "";
    }

    private Doctor BuildDoctor() => new()
    {
        DoctorId = _editing?.DoctorId ?? 0,
        FullName = NameBox.Text.Trim(),
        Specialty = SpecialtyBox.Text.Trim(),
        Phone = PhoneBox.Text.Trim(),
        Email = EmailBox.Text.Trim(),
        Note = NoteBox.Text.Trim()
    };

    public bool IsDirty()
    {
        if (!_isEditing)
        {
            return !string.IsNullOrWhiteSpace(NameBox.Text) || !string.IsNullOrWhiteSpace(SpecialtyBox.Text) ||
                   !string.IsNullOrWhiteSpace(PhoneBox.Text) || !string.IsNullOrWhiteSpace(EmailBox.Text) ||
                   !string.IsNullOrWhiteSpace(NoteBox.Text);
        }

        if (_editing == null) return false;

        bool nameChanged = NameBox.Text.Trim() != (_editing.FullName ?? "");
        bool specialtyChanged = SpecialtyBox.Text.Trim() != (_editing.Specialty ?? "");
        bool phoneChanged = PhoneBox.Text.Trim() != (_editing.Phone ?? "");
        bool emailChanged = EmailBox.Text.Trim() != (_editing.Email ?? "");
        bool noteChanged = NoteBox.Text.Trim() != (_editing.Note ?? "");

        return nameChanged || specialtyChanged || phoneChanged || emailChanged || noteChanged;
    }

    public bool HasData() => IsDirty();

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        var doctor = BuildDoctor();
        if (string.IsNullOrWhiteSpace(doctor.FullName))
        { 
            _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập họ tên bác sĩ để tiếp tục.", "Thiếu thông tin", true));
            return; 
        }

        string action = _isEditing ? "Cập nhật" : "Thêm mới";
        var confirm = new ConfirmDialog(
            $"{action} bác sĩ?",
            $"Bạn có chắc chắn muốn {action.ToLower()} thông tin bác sĩ \"{doctor.FullName}\" không?",
            _vm);

        _vm.Modal.Show(confirm, async result => {
            if (result == true)
            {
                SaveBtn.IsEnabled = false;
                bool ok = _isEditing ? await _vm.DoctorVM.UpdateAsync(doctor) : await _vm.DoctorVM.AddAsync(doctor);
                if (ok) CloseForm(true);
                else SaveBtn.IsEnabled = true;
            }
        });
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

    private void Overlay_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
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