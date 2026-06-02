using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

internal sealed class ManualTickSource : ITickSource
{
    public event EventHandler? Tick;

    public bool IsRunning { get; private set; }

    public void Start(TimeSpan interval)
    {
        IsRunning = true;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void RaiseTick()
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}
