namespace MinimalSnapTimer.Models;

public sealed class TrayContext
{
    public required Func<string> GetCurrentStatus { get; init; }

    public required Func<string> GetCurrentTimeText { get; init; }

    public required Func<bool> IsMainWindowVisible { get; init; }

    public required Action ToggleMainWindowVisibility { get; init; }

    public required Action TogglePureMode { get; init; }

    public required Action ShowPureWindow { get; init; }

    public required Action ReturnToStandardMode { get; init; }

    public required Action StartSit { get; init; }

    public required Action StartStand { get; init; }

    public required Action<string> StartPresetById { get; init; }

    public required Func<IEnumerable<TimerPreset>> GetPresets { get; init; }

    public required Func<bool> IsPaused { get; init; }

    public required Action PauseOrResume { get; init; }

    public required Action Stop { get; init; }

    public required Action Reset { get; init; }

    public required Func<bool> IsAlwaysOnTop { get; init; }

    public required Action ToggleAlwaysOnTop { get; init; }

    public required Func<bool> IsPureClickThrough { get; init; }

    public required Action<bool> SetPureClickThrough { get; init; }

    public required Action OpenSettings { get; init; }

    public required Action Exit { get; init; }
}
