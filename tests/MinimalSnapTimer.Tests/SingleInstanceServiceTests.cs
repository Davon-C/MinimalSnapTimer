using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class SingleInstanceServiceTests
{
    [Fact]
    public async Task Forwarding_DeliversArgsToPrimaryInstance()
    {
        var appId = $"MinimalSnapTimer.Tests.{Guid.NewGuid():N}";
        using var primary = new SingleInstanceService(appId);
        var acquired = primary.TryAcquire();
        Assert.True(acquired);

        var received = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var listeningTask = primary.StartListeningAsync(args =>
        {
            received.TrySetResult(args);
            return Task.CompletedTask;
        }, cts.Token);
        await Task.Delay(100);

        using var secondary = new SingleInstanceService(appId);
        var forwarded = await secondary.ForwardToPrimaryAsync(new[] { "--minutes", "5", "--pure" });
        var args = await received.Task;
        cts.Cancel();
        await listeningTask;

        Assert.True(forwarded);
        Assert.Equal(new[] { "--minutes", "5", "--pure" }, args);
    }
}
