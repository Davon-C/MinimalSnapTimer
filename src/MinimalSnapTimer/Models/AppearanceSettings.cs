namespace MinimalSnapTimer.Models;

public sealed class AppearanceSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    public double FontSize { get; set; } = 16;

    public string FontColor { get; set; } = "#FFF2F2F2";

    public string BackgroundColor { get; set; } = "#FF1C1C1C";

    public double BackgroundOpacity { get; set; } = 0.9d;

    public double WindowOpacity { get; set; } = 1.0d;

    public bool AlwaysOnTop { get; set; }

    public bool HideTaskbarButton { get; set; }

    public bool EnablePureMode { get; set; }

    public bool ShowBorder { get; set; } = true;

    public string PureFontColor { get; set; } = "#FFF5F5F5";

    public string PureBackgroundColor { get; set; } = "#FF101010";

    public double PureBackgroundOpacity { get; set; } = 0.55d;

    public bool PureClickThrough { get; set; }

    public bool LockPureWindowPosition { get; set; }
}
