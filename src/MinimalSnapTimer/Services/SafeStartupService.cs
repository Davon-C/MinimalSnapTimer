using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public static class SafeStartupService
{
    public static StartupWindowMode ResolveInitialWindowMode(AppSettings settings, CommandLineOptions options, bool trayAvailable)
    {
        var startMinimizedToTray = (settings.General.StartMinimizedToTray || options.MinimizeToTray) && trayAvailable;
        if (startMinimizedToTray)
        {
            return StartupWindowMode.HideToTray;
        }

        var startInPureMode = settings.Appearance.EnablePureMode || settings.General.LaunchInPureMode || options.StartInPureMode;
        if (startInPureMode && (!settings.Appearance.PureClickThrough || trayAvailable))
        {
            return StartupWindowMode.ShowPureWindow;
        }

        return StartupWindowMode.ShowMainWindow;
    }

    public static bool ShouldForceShowMainWindow(bool trayAvailable, bool mainWindowVisible, bool pureWindowVisible)
    {
        return !mainWindowVisible && !pureWindowVisible && !trayAvailable;
    }
}

public enum StartupWindowMode
{
    ShowMainWindow,
    ShowPureWindow,
    HideToTray
}
