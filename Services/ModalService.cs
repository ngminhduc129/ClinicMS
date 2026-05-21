using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ClinicMS.ViewModels;

namespace ClinicMS.Services;

public class ModalService : BaseViewModel
{
    private Grid? _overlay;
    private Panel? _host;
    private readonly Stack<(object content, Rectangle? dimmer, Action<bool?>? callback, Func<bool>? canClose)> _modalStack = new();

    public bool IsVisible => _modalStack.Count > 0;

    public void Register(Grid overlay, Panel host)
    {
        _overlay = overlay;
        _host = host;
        _overlay.Visibility = Visibility.Collapsed;
    }

    public void Show(object content, Action<bool?>? callback = null, Func<bool>? canClose = null)
    {
        if (_overlay == null || _host == null) return;
        if (content is not UIElement element) return;

        Rectangle? dimmer = null;

        // Nếu đã có modal, thêm backdrop TRƯỚC modal cha (z-index: app < dimmer < parent < child)
        if (_modalStack.Count > 0)
        {
            if (_modalStack.Peek().content is UIElement prev)
            {
                prev.IsHitTestVisible = false;
            }

            dimmer = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)), // ~47% black
                IsHitTestVisible = true
            };
            // Insert backdrop BEFORE parent modal để chỉ phủ app background, không chạm modal cha
            int prevIndex = 0;
            if (_modalStack.Peek().content is UIElement prevContent)
                prevIndex = _host.Children.IndexOf(prevContent);
            _host.Children.Insert(prevIndex, dimmer);
        }

        _modalStack.Push((content, dimmer, callback, canClose));
        
        // Đảm bảo phần tử được căn giữa
        if (element is FrameworkElement fe)
        {
            fe.HorizontalAlignment = HorizontalAlignment.Center;
            fe.VerticalAlignment = VerticalAlignment.Center;
        }

        _host.Children.Add(element);
        _overlay.Visibility = Visibility.Visible;
        OnPropertyChanged(nameof(IsVisible));
    }

    public void RequestClose()
    {
        if (_modalStack.Count == 0) return;
        
        var current = _modalStack.Peek();
        if (current.canClose == null || current.canClose())
        {
            Close();
        }
    }

    public void Close(bool? result = null)
    {
        if (_overlay == null || _host == null || _modalStack.Count == 0) return;

        var current = _modalStack.Pop();
        
        // Gỡ bỏ modal và lớp phủ đi kèm
        _host.Children.Remove(current.content as UIElement);
        if (current.dimmer != null)
        {
            _host.Children.Remove(current.dimmer);
        }

        // Kích hoạt lại tương tác cho modal bên dưới
        if (_modalStack.Count > 0)
        {
            if (_modalStack.Peek().content is UIElement next)
            {
                next.IsHitTestVisible = true;
            }
        }

        current.callback?.Invoke(result);

        if (_modalStack.Count == 0)
        {
            _overlay.Visibility = Visibility.Collapsed;
        }
        
        OnPropertyChanged(nameof(IsVisible));
    }
}
