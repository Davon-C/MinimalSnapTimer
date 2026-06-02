using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class SafeStartupServiceTests
{
    [Fact]
    public void ResolveInitialWindowMode_WhenTrayUnavailableAndConfiguredForTray_ShowsMainWindow()
    {
        var settings = SettingsService.CreateDefaultSettings();
        settings.General.StartMinimizedToTray = true;

        var mode = SafeStartupService.ResolveInitialWindowMode(settings, new CommandLineOptions(), trayAvailable: false);

        Assert.Equal(StartupWindowMode.ShowMainWindow, mode);
    }

    [Fact]
    public void ResolveInitialWindowMode_WhenPureModeRequested_ShowsPureWindow()
    {
        var settings = SettingsService.CreateDefaultSettings();
        settings.Appearance.EnablePureMode = true;

        var mode = SafeStartupService.ResolveInitialWindowMode(settings, new CommandLineOptions(), trayAvailable: true);

        Assert.Equal(StartupWindowMode.ShowPureWindow, mode);
    }

    [Fact]
    public void ResolveInitialWindowMode_WhenClickThroughPureModeAndTrayUnavailable_ShowsMainWindow()
    {
        var settings = SettingsService.CreateDefaultSettings();
        settings.Appearance.EnablePureMode = true;
        settings.Appearance.PureClickThrough = true;

        var mode = SafeStartupService.ResolveInitialWindowMode(settings, new CommandLineOptions(), trayAvailable: false);

        Assert.Equal(StartupWindowMode.ShowMainWindow, mode);
    }

    [Fact]
    public void ShouldForceShowMainWindow_WhenNothingIsVisibleAndTrayUnavailable_ReturnsTrue()
    {
        Assert.True(SafeStartupService.ShouldForceShowMainWindow(
            trayAvailable: false,
            mainWindowVisible: false,
            pureWindowVisible: false));
    }
}
