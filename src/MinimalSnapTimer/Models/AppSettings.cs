namespace MinimalSnapTimer.Models;

public sealed class AppSettings
{
    public int SettingsVersion { get; set; } = 2;

    public GeneralSettings General { get; set; } = new();

    public TimingSettings Timing { get; set; } = new();

    public AppearanceSettings Appearance { get; set; } = new();

    public ReminderSettings Reminder { get; set; } = new();

    public TraySettings Tray { get; set; } = new();

    public HotkeySettings Hotkeys { get; set; } = new();

    public WindowPlacementSettings Windows { get; set; } = new();

    public List<TimerPreset> Presets { get; set; } = new();
}
