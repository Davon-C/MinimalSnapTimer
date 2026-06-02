namespace MinimalSnapTimer.Tests;

public sealed class TrayServiceImplementationTests
{
    [Fact]
    public void TrayService_RebuildMenuAndTooltip_EnsureTrayIcon()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Services", "TrayService.cs"));

        Assert.Contains("public void RebuildMenu()", content);
        Assert.Contains("public void UpdateTooltip(string status, string timeText)", content);
        Assert.Contains("public void SetStateIcon(TimerState state)", content);
        Assert.Contains("public void EnsureTrayVisible()", content);
        Assert.Contains("EnsureTrayIcon();", content);
        Assert.Contains("tray.currentStatus", content);
        Assert.Contains("tray.currentTime", content);
    }

    [Fact]
    public void TrayService_NoLongerUsesSystemStateIconsForTimerStates()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Services", "TrayService.cs"));

        Assert.DoesNotContain("SystemIcons.Information", content);
        Assert.DoesNotContain("SystemIcons.Warning", content);
        Assert.DoesNotContain("SystemIcons.Error", content);
    }

    private static string GetSourcePath(params string[] parts)
    {
        var segments = new[] { AppContext.BaseDirectory, "..", "..", "..", "..", ".." }
            .Concat(parts)
            .ToArray();
        return Path.GetFullPath(Path.Combine(segments));
    }
}
