using System.Windows.Threading;

namespace MinimalSnapTimer.Services;

public sealed class DispatcherTickSource : ITickSource
{
    private readonly DispatcherTimer _timer = new();

    public DispatcherTickSource()
    {
        _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Tick;

    public bool IsRunning => _timer.IsEnabled;

    public void Start(TimeSpan interval)
    {
        _timer.Interval = interval;
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        _timer.Stop();
    }
}
