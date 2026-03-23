using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using ClinicMS.ViewModels;

namespace ClinicMS.Services;

public enum ToastType { Success, Error }

public class ToastItem : BaseViewModel
{
    public string Message { get; set; } = "";
    public ToastType Type { get; set; }
    public bool IsError => Type == ToastType.Error;
}

public class ToastService : BaseViewModel
{
    public ObservableCollection<ToastItem> Toasts { get; } = new();

    public void Show(string message, ToastType type = ToastType.Success)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var toast = new ToastItem { Message = message, Type = type };
        App.Current.Dispatcher.Invoke(() => Toasts.Add(toast));

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            App.Current.Dispatcher.Invoke(() => Toasts.Remove(toast));
        };
        timer.Start();
    }

    public void ShowError(string message) => Show(message, ToastType.Error);
}
