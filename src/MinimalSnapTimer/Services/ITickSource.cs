namespace MinimalSnapTimer.Services;

public interface ITickSource
{
    event EventHandler? Tick;

    bool IsRunning { get; }

    void Start(TimeSpan interval);

    void Stop();
}
