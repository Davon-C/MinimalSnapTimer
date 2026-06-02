namespace MinimalSnapTimer.Models;

public sealed class CommandLineOptions
{
    public TimeSpan? Duration { get; set; }

    public string? PresetName { get; set; }

    public WorkflowStage? Mode { get; set; }

    public bool AutoStart { get; set; }

    public bool StartPaused { get; set; }

    public bool StartInPureMode { get; set; }

    public bool AlwaysOnTop { get; set; }

    public bool MinimizeToTray { get; set; }

    public bool SoundOff { get; set; }

    public bool AutoRestart { get; set; }

    public bool ShowHelp { get; set; }

    public bool IsValid { get; set; } = true;

    public string? ValidationError { get; set; }
}
