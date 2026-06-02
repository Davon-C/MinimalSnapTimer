using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), "MinimalSnapTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "portable.flag"), string.Empty);

        var service = new SettingsService(root);
        var settings = SettingsService.CreateDefaultSettings();
        settings.Timing.DefaultMinutes = 55;
        settings.Appearance.EnablePureMode = true;
        settings.Appearance.Theme = MinimalSnapTimer.Models.ThemePreference.Dark;
        settings.General.Language = "en-US";
        settings.Reminder.UseToastNotifications = false;
        settings.Presets.Add(new() { Name = "\u6d4b\u8bd5\u9884\u8bbe", DurationMinutes = 7 });

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        Assert.Equal(55, loaded.Timing.DefaultMinutes);
        Assert.True(loaded.Appearance.EnablePureMode);
        Assert.Equal(MinimalSnapTimer.Models.ThemePreference.Dark, loaded.Appearance.Theme);
        Assert.Equal("en-US", loaded.General.Language);
        Assert.False(loaded.Reminder.UseToastNotifications);
        Assert.Contains(loaded.Presets, p => p.Name == "\u6d4b\u8bd5\u9884\u8bbe");
    }

    [Fact]
    public async Task LoadAsync_WhenThemeAndLanguageAreInvalid_FallsBackToSafeDefaults()
    {
        var root = Path.Combine(Path.GetTempPath(), "MinimalSnapTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "portable.flag"), string.Empty);

        var json = """
        {
          "general": {
            "language": "fr-FR"
          },
          "appearance": {
            "theme": 999
          }
        }
        """;

        File.WriteAllText(Path.Combine(root, "settings.json"), json);

        var service = new SettingsService(root);
        var loaded = await service.LoadAsync();

        Assert.Equal("zh-CN", loaded.General.Language);
        Assert.Equal(MinimalSnapTimer.Models.ThemePreference.Light, loaded.Appearance.Theme);
    }

    [Fact]
    public async Task LoadAsync_WhenSettingsFileIsBroken_BacksUpAndUsesDefaults()
    {
        var root = Path.Combine(Path.GetTempPath(), "MinimalSnapTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "portable.flag"), string.Empty);
        File.WriteAllText(Path.Combine(root, "settings.json"), "{ not-json");

        var service = new SettingsService(root);
        var loaded = await service.LoadAsync();

        Assert.Equal("zh-CN", loaded.General.Language);
        Assert.True(Directory.GetFiles(root, "settings.json.broken-*").Length > 0);
        Assert.Equal(40, loaded.Timing.DefaultMinutes);
    }

    [Fact]
    public async Task LoadAsync_WhenNewFieldsAreMissing_StillLoads()
    {
        var root = Path.Combine(Path.GetTempPath(), "MinimalSnapTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "portable.flag"), string.Empty);

        var json = """
        {
          "general": {
            "singleInstance": true
          },
          "timing": {
            "defaultMinutes": 12
          }
        }
        """;

        File.WriteAllText(Path.Combine(root, "settings.json"), json);

        var service = new SettingsService(root);
        var loaded = await service.LoadAsync();

        Assert.Equal(12, loaded.Timing.DefaultMinutes);
        Assert.Equal("zh-CN", loaded.General.Language);
        Assert.True(loaded.Reminder.UseToastNotifications);
    }

    [Fact]
    public void EnsureDefaults_WhenSettingsVersionMissing_UpgradesToCurrentVersion()
    {
        var settings = new MinimalSnapTimer.Models.AppSettings
        {
            SettingsVersion = 0
        };

        SettingsService.EnsureDefaults(settings);

        Assert.Equal(SettingsService.CurrentSettingsVersion, settings.SettingsVersion);
    }
}
