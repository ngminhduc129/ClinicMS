using System;
using System.Linq;
using System.Windows;

namespace ClinicMS.Services;

public static class ThemeManager
{
    public enum AppTheme { Light, Dark }

    public static void ApplyTheme(AppTheme theme)
    {
        var dict = Application.Current.Resources.MergedDictionaries;
        
        // 1. Find the ClinicTheme.xaml (the master theme)
        var masterTheme = dict.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("ClinicTheme.xaml"));
        if (masterTheme == null) return;

        // 2. Find and replace the ThemeColors.xaml inside the master theme's merged dictionaries
        var subDicts = masterTheme.MergedDictionaries;
        var colorDict = subDicts.FirstOrDefault(d => d.Source != null && (d.Source.OriginalString.Contains("ThemeColors.xaml") || d.Source.OriginalString.Contains("ThemeColorsDark.xaml")));
        
        string newSource = theme == AppTheme.Dark 
            ? "pack://application:,,,/Resources/Themes/ThemeColorsDark.xaml" 
            : "pack://application:,,,/Resources/Themes/ThemeColors.xaml";

        if (colorDict != null)
        {
            if (colorDict.Source.OriginalString == newSource) return; // Already applied
            subDicts.Remove(colorDict);
        }

        subDicts.Insert(0, new ResourceDictionary { Source = new Uri(newSource) });
    }

    public static AppTheme CurrentTheme
    {
        get
        {
            var masterTheme = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("ClinicTheme.xaml"));
            if (masterTheme == null) return AppTheme.Light;

            var colorDict = masterTheme.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("ThemeColorsDark.xaml"));
            return colorDict != null ? AppTheme.Dark : AppTheme.Light;
        }
    }
}
