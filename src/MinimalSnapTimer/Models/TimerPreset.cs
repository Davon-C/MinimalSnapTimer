namespace MinimalSnapTimer.Models;

public sealed class TimerPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public PresetType Type { get; set; } = PresetType.Normal;

    public bool AutoStart { get; set; } = true;

    public CompletionAction CompletionAction { get; set; } = CompletionAction.None;

    public bool AutoSwitchToNext { get; set; }

    public string? NextPresetId { get; set; }
}
