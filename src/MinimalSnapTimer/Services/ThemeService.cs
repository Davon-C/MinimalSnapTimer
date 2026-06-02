using Microsoft.Win32;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class ThemeService
{
    private const string BaseThemeSource = "pack://application:,,,/MinimalSnapTimer;component/Themes/BaseTheme.xaml";
    private const string LightThemeSource = "pack://application:,,,/MinimalSnapTimer;component/Themes/LightTheme.xaml";
    private const string DarkThemeSource = "pack://application:,,,/MinimalSnapTimer;component/Themes/DarkTheme.xaml";

    public static ThemeService Instance { get; } = new();

    public Func<Uri, System.Windows.ResourceDictionary> ResourceDictionaryFactory { get; set; } =
        uri => new System.Windows.ResourceDictionary { Source = uri };

    public ThemePreference RequestedTheme { get; private set; } = ThemePreference.System;

    public ThemePreference EffectiveTheme { get; private set; } = ThemePreference.Light;

    public bool TryApplyTheme(ThemePreference preference, Action<string>? log = null)
    {
        RequestedTheme = NormalizePreference(preference);

        try
        {
            EffectiveTheme = ResolveEffectiveTheme(RequestedTheme, SystemUsesDarkTheme());
            if (System.Windows.Application.Current is null)
            {
                return true;
            }

            EnsureBaseTheme();
            ReplaceThemeDictionary(GetThemeUri(EffectiveTheme));
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"主题加载失败，已回退浅色主题。{Environment.NewLine}{ex}");

            try
            {
                RequestedTheme = ThemePreference.Light;
                EffectiveTheme = ThemePreference.Light;
                if (System.Windows.Application.Current is not null)
                {
                    EnsureBaseTheme();
                    ReplaceThemeDictionary(GetThemeUri(ThemePreference.Light));
                }
            }
            catch
            {
            }

            return false;
        }
    }

    public void ApplyTheme(ThemePreference preference)
    {
        TryApplyTheme(preference);
    }

    public static ThemePreference ResolveEffectiveTheme(ThemePreference preference, bool systemUsesDarkTheme)
    {
        var normalized = NormalizePreference(preference);
        return normalized switch
        {
            ThemePreference.Light => ThemePreference.Light,
            ThemePreference.Dark => ThemePreference.Dark,
            _ => systemUsesDarkTheme ? ThemePreference.Dark : ThemePreference.Light
        };
    }

    public static bool SystemUsesDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0;
            }
        }
        catch
        {
        }

        return false;
    }

    private static ThemePreference NormalizePreference(ThemePreference preference)
    {
        return Enum.IsDefined(typeof(ThemePreference), preference) ? preference : ThemePreference.Light;
    }

    private static Uri GetThemeUri(ThemePreference preference)
    {
        var source = preference == ThemePreference.Dark ? DarkThemeSource : LightThemeSource;
        return new Uri(source, UriKind.Absolute);
    }

    private void EnsureBaseTheme()
    {
        var merged = System.Windows.Application.Current!.Resources.MergedDictionaries;
        if (merged.Any(dictionary => string.Equals(dictionary.Source?.OriginalString, BaseThemeSource, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        merged.Insert(0, ResourceDictionaryFactory(new Uri(BaseThemeSource, UriKind.Absolute)));
    }

    private void ReplaceThemeDictionary(Uri source)
    {
        var merged = System.Windows.Application.Current!.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(dictionary =>
            string.Equals(dictionary.Source?.OriginalString, LightThemeSource, StringComparison.OrdinalIgnoreCase)
            || string.Equals(dictionary.Source?.OriginalString, DarkThemeSource, StringComparison.OrdinalIgnoreCase));

        var replacement = ResourceDictionaryFactory(source);

        if (existing is null)
        {
            merged.Add(replacement);
            return;
        }

        var index = merged.IndexOf(existing);
        merged[index] = replacement;
    }
}
