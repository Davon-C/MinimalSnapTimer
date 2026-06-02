namespace MinimalSnapTimer.Models;

public sealed class TimerCompletedEventArgs : EventArgs
{
    public TimerCompletedEventArgs(TimerSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public TimerSnapshot Snapshot { get; }
}
