using System;
using System.Windows;
using System.Windows.Controls;

namespace ClinicMS.Services;

public class ModalService
{
    private Grid? _overlay;
    private ContentPresenter? _host;
    private Action<bool?>? _callback;
    private Func<bool>? _hasData;

    public void Register(Grid overlay, ContentPresenter host)
    {
        _overlay = overlay;
        _host    = host;
    }

    public void Show(UserControl content, Action<bool?>? onClose = null, Func<bool>? hasData = null)
    {
        if (_overlay == null || _host == null) return;
        _callback      = onClose;
        _hasData       = hasData;
        _host.Content  = content;
        _overlay.Visibility = Visibility.Visible;
    }

    public void Close(bool? result = null)
    {
        if (_overlay == null || _host == null) return;
        _overlay.Visibility = Visibility.Collapsed;
        _host.Content       = null;
        _callback?.Invoke(result);
        _callback = null;
        _hasData  = null;
    }

    public void RequestClose()
    {
        if (_hasData?.Invoke() == true) return;
        Close();
    }
}
