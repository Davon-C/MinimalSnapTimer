using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using System.Windows;

namespace MinimalSnapTimer.Tests;

public sealed class ThemeServiceTests
{
    [Theory]
    [InlineData(ThemePreference.Light, true, ThemePreference.Light)]
    [InlineData(ThemePreference.Dark, false, ThemePreference.Dark)]
    [InlineData(ThemePreference.System, true, ThemePreference.Dark)]
    [InlineData(ThemePreference.System, false, ThemePreference.Light)]
    public void ResolveEffectiveTheme_ReturnsExpectedTheme(ThemePreference preference, bool systemUsesDarkTheme, ThemePreference expected)
    {
        var actual = ThemeService.ResolveEffectiveTheme(preference, systemUsesDarkTheme);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryApplyTheme_WhenFactoryThrows_FallsBackWithoutThrowing()
    {
        _ = Application.Current ?? new Application();
        var service = new ThemeService
        {
            ResourceDictionaryFactory = _ => throw new InvalidOperationException("theme load failed")
        };

        var result = service.TryApplyTheme((ThemePreference)999);

        Assert.False(result);
        Assert.Equal(ThemePreference.Light, service.EffectiveTheme);
    }

    [Fact]
    public void DarkThemeDictionary_ContainsReadableBrushResources()
    {
        _ = Application.Current ?? new Application();
        var dictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MinimalSnapTimer;component/Themes/DarkTheme.xaml", UriKind.Absolute)
        };

        Assert.NotNull(dictionary["ControlForegroundBrush"]);
        Assert.NotNull(dictionary["ControlBackgroundBrush"]);
        Assert.NotNull(dictionary["BorderBrush"]);
        Assert.NotNull(dictionary["SelectionBackgroundBrush"]);
        Assert.NotNull(dictionary["DisabledForegroundBrush"]);
        Assert.NotNull(dictionary["AppPureToolbarBackgroundBrush"]);
    }

    [Fact]
    public void LightThemeDictionary_ContainsReadableBrushResources()
    {
        _ = Application.Current ?? new Application();
        var dictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MinimalSnapTimer;component/Themes/LightTheme.xaml", UriKind.Absolute)
        };

        Assert.NotNull(dictionary["ControlForegroundBrush"]);
        Assert.NotNull(dictionary["ControlBackgroundBrush"]);
        Assert.NotNull(dictionary["BorderBrush"]);
        Assert.NotNull(dictionary["SelectionBackgroundBrush"]);
        Assert.NotNull(dictionary["DisabledForegroundBrush"]);
        Assert.NotNull(dictionary["AppPureToolbarBackgroundBrush"]);
    }

    [Fact]
    public void ApplyTheme_ReplacesExistingThemeDictionary_InsteadOfGrowingMergedDictionaries()
    {
        var app = Application.Current ?? new Application();
        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MinimalSnapTimer;component/Themes/BaseTheme.xaml", UriKind.Absolute)
        });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MinimalSnapTimer;component/Themes/LightTheme.xaml", UriKind.Absolute)
        });

        var service = new ThemeService();
        service.ApplyTheme(ThemePreference.Dark);
        service.ApplyTheme(ThemePreference.Light);

        var merged = app.Resources.MergedDictionaries;
        var themeCount = merged.Count(d =>
            string.Equals(d.Source?.OriginalString, "pack://application:,,,/MinimalSnapTimer;component/Themes/LightTheme.xaml", StringComparison.OrdinalIgnoreCase)
            || string.Equals(d.Source?.OriginalString, "pack://application:,,,/MinimalSnapTimer;component/Themes/DarkTheme.xaml", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, themeCount);
    }
}
