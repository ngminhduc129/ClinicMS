using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class MedicineFormDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Medicine?     _editing;
    private bool _isEditing => _editing != null;

    public MedicineFormDialog(MainViewModel vm, Medicine? m = null)
    {
        InitializeComponent();
        _vm      = vm;
        _editing = m;
        Opacity = 0;

        if (_isEditing && m != null)
        {
            TitleText.Text = "Sửa Thông Tin Thuốc";
            SubtitleText.Text = "Cập nhật thông tin dược phẩm";
            NameBox.Text    = m.MedicineName;
            UnitBox.Text    = m.Unit         ?? "";
            PriceBox.Text   = m.Price.ToString();
            StockBox.Text   = m.StockQuantity.ToString();

            Loaded += (_, _) =>
            {
                if (SaveBtn.Template?.FindName("SaveLabel", SaveBtn) is TextBlock lbl)
                    lbl.Text = "Cập Nhật";
            };
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

    public bool IsDirty()
    {
        if (!_isEditing)
        {
            return !string.IsNullOrWhiteSpace(NameBox.Text) ||
                   !string.IsNullOrWhiteSpace(UnitBox.Text) ||
                   !string.IsNullOrWhiteSpace(PriceBox.Text) ||
                   !string.IsNullOrWhiteSpace(StockBox.Text);
        }

        if (_editing == null) return false;

        bool nameChanged = NameBox.Text.Trim() != (_editing.MedicineName ?? "");
        bool unitChanged = UnitBox.Text.Trim() != (_editing.Unit ?? "");
        bool priceChanged = PriceBox.Text.Trim() != (_editing.Price.ToString());
        bool stockChanged = StockBox.Text.Trim() != (_editing.StockQuantity.ToString());

        return nameChanged || unitChanged || priceChanged || stockChanged;
    }

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) { MessageBox.Show("Vui lòng nhập tên thuốc.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        
        SaveBtn.IsEnabled = false;
        var m = new Medicine
        {
            MedicineId    = _editing?.MedicineId ?? 0,
            MedicineName  = name,
            Unit          = UnitBox.Text.Trim(),
            Price         = double.TryParse(PriceBox.Text.Trim(), out var p) ? p : 0,
            StockQuantity = int.TryParse(StockBox.Text.Trim(), out var q) ? q : 0
        };

        bool ok = !_isEditing
            ? await _vm.AddMedicineAsync(m)
            : await _vm.UpdateMedicineAsync(m);

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

    private void Overlay_Click(object s, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not Grid g || g.Name != "Overlay") return;
        
        if (!IsDirty())
        {
            CloseForm();
            return;
        }

        var result = MessageBox.Show(
            "Thông tin đã thay đổi sẽ bị mất. Bạn có chắc muốn thoát không?",
            "Thoát form?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) CloseForm();
    }

    public bool HasData() => IsDirty();
}
