using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClinicMS.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace ClinicMS.Views;

public partial class PdfPreviewView : UserControl
{
    private readonly MainViewModel _mainVm;
    private readonly string _filePath;

    public PdfPreviewView(string filePath, string title, MainViewModel mainVm)
    {
        InitializeComponent();
        _mainVm = mainVm;
        _filePath = filePath;
        
        TxtTitle.Text = title;
        TxtFileName.Text = Path.GetFileName(filePath);

        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        try
        {
            // Đợi WebView2 khởi tạo xong nhân (CoreWebView2)
            await PdfWebView.EnsureCoreWebView2Async(null);
            
            // Tắt các tính năng không cần thiết để giao diện sạch sẽ
            PdfWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            PdfWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            // Load file PDF
            PdfWebView.Source = new Uri(_filePath);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Lỗi khi khởi tạo WebView2: {FilePath}", _filePath);
            MessageBox.Show("Không thể khởi tạo trình xem PDF. Vui lòng đảm bảo máy tính đã cài đặt Microsoft Edge WebView2 Runtime.", "Lỗi Giao Diện", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _mainVm.Modal.Close();
    }
}
