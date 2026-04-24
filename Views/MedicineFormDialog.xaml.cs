using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class MedicineFormDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Medicine? _editing;
    private bool _isEditing => _editing != null;

    public MedicineFormDialog(MainViewModel vm, Medicine? medicine = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = medicine;
        Opacity = 0;

        if (_isEditing && medicine != null)
        {
            TitleText.Text = "Sửa Thông Tin Thuốc";
            SubtitleText.Text = "Cập nhật đơn giá và số lượng tồn kho";
            Loaded += (_, _) =>
            {
                if (SaveBtn.Template?.FindName("SaveLabel", SaveBtn) is TextBlock lbl)
                    lbl.Text = "Cập Nhật";
            };
            LoadData(medicine);
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

        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, dur) { EasingFunction = ease });
        translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(40, 0, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease });
    }

    private void CloseForm(bool? result = null) => _vm.Modal.Close(result);

    private void LoadData(Medicine m)
    {
        NameBox.Text = m.MedicineName ?? "";
        UnitBox.Text = m.Unit ?? "";
        PriceBox.Text = m.Price.ToString();
        StockBox.Text = m.StockQuantity.ToString();
    }

    private Medicine BuildMedicine() => new()
    {
        MedicineId = _editing?.MedicineId ?? 0,
        MedicineName = NameBox.Text.Trim(),
        Unit = UnitBox.Text.Trim(),
        Price = double.TryParse(PriceBox.Text.Trim(), out var p) ? p : 0,
        StockQuantity = double.TryParse(StockBox.Text.Trim(), out var s) ? (int)s : 0,
        CreatedAt = _editing?.CreatedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    };

    public bool IsDirty()
    {
        if (!_isEditing) return !string.IsNullOrWhiteSpace(NameBox.Text) || !string.IsNullOrWhiteSpace(UnitBox.Text) || !string.IsNullOrWhiteSpace(PriceBox.Text) || !string.IsNullOrWhiteSpace(StockBox.Text);
        if (_editing == null) return false;
        return NameBox.Text.Trim() != (_editing.MedicineName ?? "") || UnitBox.Text.Trim() != (_editing.Unit ?? "") || PriceBox.Text.Trim() != _editing.Price.ToString() || StockBox.Text.Trim() != _editing.StockQuantity.ToString();
    }

    public bool HasData() => IsDirty();

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        var med = BuildMedicine();
        if (string.IsNullOrWhiteSpace(med.MedicineName))
        {
            _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập tên thuốc.", "Thiếu thông tin", true));
            return;
        }

        string action = _isEditing ? "Cập nhật" : "Thêm mới";
        var confirm = new ConfirmDialog(
            $"{action} thuốc?",
            $"Bạn có chắc chắn muốn {action.ToLower()} thông tin thuốc \"{med.MedicineName}\" không?",
            _vm);

        _vm.Modal.Show(confirm, async result => {
            if (result == true)
            {
                SaveBtn.IsEnabled = false;
                bool ok = _isEditing ? await _vm.MedicineVM.UpdateAsync(med) : await _vm.MedicineVM.AddAsync(med);
                if (ok) CloseForm(true);
                else SaveBtn.IsEnabled = true;
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

    private void Close_Click(object s, RoutedEventArgs e) => Cancel_Click(s, e);
    
    private void Overlay_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Grid g && g.Name == "Overlay") Cancel_Click(s, null!);
    }
}
