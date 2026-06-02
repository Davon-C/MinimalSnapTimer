using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class TimerEngine
{
    private readonly ITickSource _tickSource;
    private readonly TimeSpan _tickInterval;
    private TimerMode _mode = TimerMode.Countdown;
    private TimerState _state = TimerState.Idle;
    private WorkflowStage _stage = WorkflowStage.None;
    private TimeSpan _initialDuration = TimeSpan.Zero;
    private TimeSpan _currentTime = TimeSpan.Zero;
    private string? _presetId;

    public TimerEngine(ITickSource tickSource, TimeSpan? tickInterval = null)
    {
        _tickSource = tickSource;
        _tickInterval = tickInterval ?? TimeSpan.FromSeconds(1);
        _tickSource.Tick += (_, _) => Advance(_tickInterval);
    }

    public event EventHandler<TimerSnapshot>? SnapshotChanged;

    public event EventHandler<TimerCompletedEventArgs>? Completed;

    public TimerSnapshot Snapshot => new()
    {
        Mode = _mode,
        State = _state,
        Stage = _stage,
        InitialDuration = _initialDuration,
        CurrentTime = _currentTime,
        PresetId = _presetId
    };

    public void Start(TimeSpan duration, WorkflowStage stage = WorkflowStage.None, string? presetId = null, bool startPaused = false)
    {
        _stage = stage;
        _presetId = presetId;
        _initialDuration = duration <= TimeSpan.Zero ? TimeSpan.Zero : duration;
        _mode = duration <= TimeSpan.Zero ? TimerMode.Stopwatch : TimerMode.Countdown;
        _currentTime = _mode == TimerMode.Countdown ? _initialDuration : TimeSpan.Zero;
        _state = startPaused ? TimerState.Paused : TimerState.Running;

        if (startPaused)
        {
            _tickSource.Stop();
        }
        else
        {
            _tickSource.Start(_tickInterval);
        }

        RaiseSnapshotChanged();
    }

    public void Pause()
    {
        if (_state != TimerState.Running)
        {
            return;
        }

        _state = TimerState.Paused;
        _tickSource.Stop();
        RaiseSnapshotChanged();
    }

    public void Resume()
    {
        if (_state != TimerState.Paused)
        {
            return;
        }

        _state = TimerState.Running;
        _tickSource.Start(_tickInterval);
        RaiseSnapshotChanged();
    }

    public void TogglePauseResume()
    {
        if (_state == TimerState.Running)
        {
            Pause();
            return;
        }

        if (_state == TimerState.Paused)
        {
            Resume();
        }
    }

    public void Stop()
    {
        _tickSource.Stop();
        _state = TimerState.Idle;
        _mode = TimerMode.Countdown;
        _currentTime = _initialDuration;
        _stage = WorkflowStage.None;
        _presetId = null;
        RaiseSnapshotChanged();
    }

    public void Reset()
    {
        _tickSource.Stop();
        _state = TimerState.Idle;
        _currentTime = _mode == TimerMode.Countdown ? _initialDuration : TimeSpan.Zero;
        RaiseSnapshotChanged();
    }

    public void Advance(TimeSpan delta)
    {
        if (_state != TimerState.Running)
        {
            return;
        }

        if (_mode == TimerMode.Stopwatch)
        {
            _currentTime += delta;
            RaiseSnapshotChanged();
            return;
        }

        _currentTime -= delta;

        if (_currentTime <= TimeSpan.Zero)
        {
            _currentTime = TimeSpan.Zero;
            _state = TimerState.Completed;
            _tickSource.Stop();
            var snapshot = Snapshot;
            RaiseSnapshotChanged(snapshot);
            Completed?.Invoke(this, new TimerCompletedEventArgs(snapshot));
            return;
        }

        RaiseSnapshotChanged();
    }

    private void RaiseSnapshotChanged()
    {
        RaiseSnapshotChanged(Snapshot);
    }

    private void RaiseSnapshotChanged(TimerSnapshot snapshot)
    {
        SnapshotChanged?.Invoke(this, snapshot);
    }
}
