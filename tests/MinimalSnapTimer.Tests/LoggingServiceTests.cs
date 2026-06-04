using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class LoggingServiceTests
{
    [Fact]
    public void Write_WhenTooManyLogsExist_TrimsOldFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "MinimalSnapTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        for (var i = 0; i < 35; i++)
        {
            File.WriteAllText(Path.Combine(root, $"202601{i:00}.log"), "old");
        }

        var service = new LoggingService(root);
        service.Write("test");

        var files = Directory.GetFiles(root, "*.log");
        Assert.True(files.Length < 35);
        Assert.DoesNotContain(Path.Combine(root, "20260100.log"), files, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_WhenCurrentLogIsTooLarge_RotatesFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "MinimalSnapTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var todayPath = Path.Combine(root, $"{DateTime.Now:yyyyMMdd}.log");
        File.WriteAllText(todayPath, new string('A', 600_000));

        var service = new LoggingService(root);
        service.Write("rotation");

        Assert.True(File.Exists(todayPath));
        Assert.Contains(Directory.GetFiles(root, "*.log"), path => !string.Equals(path, todayPath, StringComparison.OrdinalIgnoreCase));
    }
}
