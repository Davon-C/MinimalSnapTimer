namespace MinimalSnapTimer.Models;

public sealed class TimingSettings
{
    public int DefaultMinutes { get; set; } = 40;

    public int SitMinutes { get; set; } = 40;

    public int StandMinutes { get; set; } = 15;

    public bool ShowSeconds { get; set; } = true;

    public bool AutoRestart { get; set; }

    public bool AutoEnterNextPreset { get; set; }

    public bool EnableStopwatchMode { get; set; } = true;

    public bool AutoCycleWorkflow { get; set; }

    public bool WaitForUserBeforeNextStage { get; set; } = true;

    public string RecentTimer { get; set; } = "00:40:00";
}
