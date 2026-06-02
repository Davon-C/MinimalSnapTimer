namespace MinimalSnapTimer.Models;

public sealed class TimerSnapshot
{
    public TimerMode Mode { get; init; } = TimerMode.Countdown;

    public TimerState State { get; init; } = TimerState.Idle;

    public WorkflowStage Stage { get; init; } = WorkflowStage.None;

    public TimeSpan InitialDuration { get; init; }

    public TimeSpan CurrentTime { get; init; }

    public string? PresetId { get; init; }

    public bool IsCompleted => State == TimerState.Completed;
}
