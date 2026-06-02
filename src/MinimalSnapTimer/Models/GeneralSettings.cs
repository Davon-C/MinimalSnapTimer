namespace MinimalSnapTimer.Models;

public sealed class GeneralSettings
{
    public bool AutoStartOnLaunch { get; set; }

    public bool LaunchInPureMode { get; set; }

    public bool StartMinimizedToTray { get; set; }

    public CloseBehavior CloseBehavior { get; set; } = CloseBehavior.MinimizeToTray;

    public bool SingleInstance { get; set; } = true;

    public bool AutoStartWithWindows { get; set; }

    public string Language { get; set; } = "zh-CN";

    public bool EnableLocalLogs { get; set; }
}
