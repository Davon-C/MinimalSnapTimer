namespace MinimalSnapTimer.Models;

public sealed class TraySettings
{
    public bool MinimizeToTray { get; set; } = true;

    public bool ShowRemainingInTooltip { get; set; } = true;

    public bool ShowPresetsInMenu { get; set; } = true;

    public bool ShowStatusIcon { get; set; } = true;
}
