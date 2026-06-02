using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class TimerEngineTests
{
    [Fact]
    public void Countdown_Decrements_WhenAdvanced()
    {
        var tick = new ManualTickSource();
        var engine = new TimerEngine(tick);

        engine.Start(TimeSpan.FromMinutes(1));
        engine.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(50), engine.Snapshot.CurrentTime);
        Assert.Equal(TimerState.Running, engine.Snapshot.State);
    }

    [Fact]
    public void Pause_StopsCountdown()
    {
        var tick = new ManualTickSource();
        var engine = new TimerEngine(tick);

        engine.Start(TimeSpan.FromMinutes(1));
        engine.Pause();
        engine.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.FromMinutes(1), engine.Snapshot.CurrentTime);
        Assert.Equal(TimerState.Paused, engine.Snapshot.State);
    }

    [Fact]
    public void Resume_ContinuesCountdown()
    {
        var tick = new ManualTickSource();
        var engine = new TimerEngine(tick);

        engine.Start(TimeSpan.FromMinutes(1));
        engine.Pause();
        engine.Resume();
        engine.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(50), engine.Snapshot.CurrentTime);
        Assert.Equal(TimerState.Running, engine.Snapshot.State);
    }

    [Fact]
    public void Reset_RestoresInitialTime()
    {
        var tick = new ManualTickSource();
        var engine = new TimerEngine(tick);

        engine.Start(TimeSpan.FromMinutes(1));
        engine.Advance(TimeSpan.FromSeconds(25));
        engine.Reset();

        Assert.Equal(TimeSpan.FromMinutes(1), engine.Snapshot.CurrentTime);
        Assert.Equal(TimerState.Idle, engine.Snapshot.State);
    }

    [Fact]
    public void ZeroDuration_StartsStopwatchMode()
    {
        var tick = new ManualTickSource();
        var engine = new TimerEngine(tick);

        engine.Start(TimeSpan.Zero);
        engine.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(TimerMode.Stopwatch, engine.Snapshot.Mode);
        Assert.Equal(TimeSpan.FromSeconds(5), engine.Snapshot.CurrentTime);
    }
}
