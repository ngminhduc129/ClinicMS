using System;
using System.Linq;
using System.Windows;

namespace ClinicMS.Services;

public static class ThemeManager
{
    public enum AppTheme { Light, Dark }

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static void ApplyTheme(AppTheme theme)
    {
        var dict = Application.Current.Resources.MergedDictionaries;

        var masterTheme = dict.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("ClinicTheme.xaml"));
        if (masterTheme == null) return;

        var subDicts = masterTheme.MergedDictionaries;
        var colorDict = subDicts.FirstOrDefault(d => d.Source != null && (d.Source.OriginalString.Contains("ThemeColors.xaml") || d.Source.OriginalString.Contains("ThemeColorsDark.xaml")));

        string newSource = theme == AppTheme.Dark
            ? "pack://application:,,,/Resources/Themes/ThemeColorsDark.xaml"
            : "pack://application:,,,/Resources/Themes/ThemeColors.xaml";

        if (colorDict != null && colorDict.Source.OriginalString == newSource)
            return;

        // Keep the same dictionary object — DynamicResource tracks by identity
        // Clear + reload from new source so WPF change notifications fire properly
        if (colorDict == null)
        {
            colorDict = new ResourceDictionary();
            subDicts.Insert(0, colorDict);
        }

        colorDict.Clear();
        colorDict.Source = new Uri(newSource);

        CurrentTheme = theme;
    }
}
