using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClinicMS.Models;
using ClinicMS.ViewModels;

namespace ClinicMS.Views;

public partial class ServiceFormDialog : UserControl
{
    private readonly MainViewModel _vm;
    private readonly Service? _editing;
    private bool _isEditing => _editing != null;

    public ServiceFormDialog(MainViewModel vm, Service? service = null)
    {
        InitializeComponent();
        _vm = vm;
        _editing = service;
        Opacity = 0;

        if (_isEditing && service != null)
        {
            TitleText.Text = "Sửa Thông Tin Dịch Vụ";
            SubtitleText.Text = "Cập nhật tên dịch vụ và đơn giá";
            Loaded += (_, _) =>
            {
                if (SaveBtn.Template?.FindName("SaveLabel", SaveBtn) is TextBlock lbl)
                    lbl.Text = "Cập Nhật";
            };
            LoadData(service);
        }

        Loaded += (_, _) => AnimateOpen();
    }

    private void AnimateOpen()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var dur = TimeSpan.FromSeconds(0.4);

        var translate = new TranslateTransform(0, 40);
        var scale = new ScaleTransform(0.96, 0.96);
        var group = new TransformGroup();
        group.Children.Add(scale);
        group.Children.Add(translate);
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = group;

        var animOpacity = new DoubleAnimation(0, 1, dur) { EasingFunction = ease };
        var animTranslate = new DoubleAnimation(40, 0, dur) { EasingFunction = ease };
        var animScaleX = new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease };
        var animScaleY = new DoubleAnimation(0.96, 1, dur) { EasingFunction = ease };

        Timeline.SetDesiredFrameRate(animOpacity, 60);
        Timeline.SetDesiredFrameRate(animTranslate, 60);
        Timeline.SetDesiredFrameRate(animScaleX, 60);
        Timeline.SetDesiredFrameRate(animScaleY, 60);

        animOpacity.Freeze();
        animTranslate.Freeze();
        animScaleX.Freeze();
        animScaleY.Freeze();

        BeginAnimation(OpacityProperty, animOpacity);
        translate.BeginAnimation(TranslateTransform.YProperty, animTranslate);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
    }

    private void CloseForm(bool? result = null) => _vm.Modal.Close(result);

    private void LoadData(Service s)
    {
        NameBox.Text = s.ServiceName ?? "";
        PriceBox.Text = s.Price.ToString();
    }

    private Service BuildService() => new()
    {
        ServiceId = _editing?.ServiceId ?? 0,
        ServiceName = NameBox.Text.Trim(),
        Price = double.TryParse(PriceBox.Text.Trim(), out var p) ? p : 0
    };

    public bool IsDirty()
    {
        if (!_isEditing) return !string.IsNullOrWhiteSpace(NameBox.Text) || !string.IsNullOrWhiteSpace(PriceBox.Text);
        if (_editing == null) return false;
        return NameBox.Text.Trim() != (_editing.ServiceName ?? "") || PriceBox.Text.Trim() != _editing.Price.ToString();
    }

    public bool HasData() => IsDirty();

    private async void Save_Click(object s, RoutedEventArgs e)
    {
        var svc = BuildService();
        if (string.IsNullOrWhiteSpace(svc.ServiceName))
        {
            _vm.Modal.Show(new MessageDialog(_vm, "Vui lòng nhập tên dịch vụ.", "Thiếu thông tin", true));
            return;
        }

        string action = _isEditing ? "Cập nhật" : "Thêm mới";
        var confirm = new ConfirmDialog(
            $"{action} dịch vụ?",
            $"Bạn có chắc chắn muốn {action.ToLower()} dịch vụ \"{svc.ServiceName}\" không?",
            _vm);

        _vm.Modal.Show(confirm, async result => {
            if (result == true)
            {
                SaveBtn.IsEnabled = false;
                bool ok = _isEditing ? await _vm.ServiceVM.UpdateAsync(svc) : await _vm.ServiceVM.AddAsync(svc);
                if (ok) CloseForm(true);
                else 
                {
                    SaveBtn.IsEnabled = true;
                    _vm.Modal.Show(new MessageDialog(_vm, "Không thể lưu thông tin dịch vụ. Vui lòng kiểm tra lại thông tin và kết nối.", "Lỗi lưu dữ liệu", true));
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

    private void Close_Click(object s, RoutedEventArgs e) => Cancel_Click(s, e);
    
    private void Overlay_Click(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.Source == e.OriginalSource || (e.OriginalSource is FrameworkElement fe && fe.Name != "DialogRoot" && FindParent<Border>(fe)?.Name != "DialogRoot"))
        {
            Cancel_Click(s, null!);
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
