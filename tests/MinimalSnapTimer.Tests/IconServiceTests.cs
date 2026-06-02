using System.Drawing;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class IconServiceTests
{
    [Fact]
    public void LoadTrayIcon_UsesExecutableIconWhenAvailable()
    {
        var service = new IconService(
            processPathProvider: () => Environment.ProcessPath,
            resourceStreamFactory: () => null);

        using var icon = service.LoadTrayIcon();

        Assert.NotNull(icon);
    }

    [Fact]
    public void LoadTrayIcon_FallsBackToResourceWhenExecutableIconUnavailable()
    {
        var service = new IconService(
            processPathProvider: () => "C:\\not-found\\MinimalSnapTimer.exe",
            resourceStreamFactory: CreateIconStream);

        using var icon = service.LoadTrayIcon();

        Assert.NotNull(icon);
    }

    [Fact]
    public void LoadTrayIcon_FallsBackToSystemIconWhenAllSourcesFail()
    {
        var logs = new List<string>();
        var service = new IconService(
            processPathProvider: () => "C:\\not-found\\MinimalSnapTimer.exe",
            resourceStreamFactory: () => throw new InvalidOperationException("boom"));

        using var icon = service.LoadTrayIcon((message, _) => logs.Add(message));

        Assert.NotNull(icon);
        Assert.Contains(logs, message => message.Contains("回退到系统默认应用图标", StringComparison.Ordinal));
    }

    private static MemoryStream CreateIconStream()
    {
        var stream = new MemoryStream();
        SystemIcons.Application.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
