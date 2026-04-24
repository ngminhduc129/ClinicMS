using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ClinicMS.ViewModels;

namespace ClinicMS.Services;

public class ModalService : BaseViewModel
{
    private Grid? _overlay;
    private ContentPresenter? _host;
    private readonly Stack<(object content, Action<bool?>? callback, Func<bool>? canClose)> _modalStack = new();

    public bool IsVisible => _modalStack.Count > 0;

    public void Register(Grid overlay, ContentPresenter host)
    {
        _overlay = overlay;
        _host = host;
        _overlay.Visibility = Visibility.Collapsed;
    }

    public void Show(object content, Action<bool?>? callback = null, Func<bool>? canClose = null)
    {
        if (_overlay == null || _host == null) return;

        _modalStack.Push((content, callback, canClose));
        
        _host.Content = content;
        _overlay.Visibility = Visibility.Visible;
        OnPropertyChanged(nameof(IsVisible));
    }

    public void RequestClose()
    {
        if (_modalStack.Count == 0) return;
        
        var current = _modalStack.Peek();
        // Kiểm tra xem hộp thoại hiện tại có cho phép đóng khi click ra ngoài không
        if (current.canClose == null || current.canClose())
        {
            Close();
        }
    }

    public void Close(bool? result = null)
    {
        if (_overlay == null || _host == null || _modalStack.Count == 0) return;

        var current = _modalStack.Pop();
        current.callback?.Invoke(result);

        if (_modalStack.Count > 0)
        {
            var previous = _modalStack.Peek();
            _host.Content = previous.content;
        }
        else
        {
            _host.Content = null;
            _overlay.Visibility = Visibility.Collapsed;
        }
        
        OnPropertyChanged(nameof(IsVisible));
    }
}
