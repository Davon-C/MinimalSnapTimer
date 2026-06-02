namespace MinimalSnapTimer.Tests;

public sealed class PureTimerWindowLayoutTests
{
    [Fact]
    public void PureTimerWindow_DoesNotContainAlwaysVisibleTopLeftBadge()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Views", "PureTimerWindow.xaml"));

        Assert.DoesNotContain("HorizontalAlignment=\"Left\"", content);
        Assert.DoesNotContain("VerticalAlignment=\"Top\"", content);
    }

    [Fact]
    public void PureTimerWindow_Toolbar_RemainsCollapsedByDefault_AndContainsStatusText()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Views", "PureTimerWindow.xaml"));

        Assert.Contains("x:Name=\"ToolbarBorder\"", content);
        Assert.Contains("Visibility=\"Collapsed\"", content);
        Assert.Contains("Text=\"{Binding StatusText}\"", content);
        Assert.Contains("Path=[pure.modeBadge]", content);
    }

    private static string GetSourcePath(params string[] parts)
    {
        var segments = new[] { AppContext.BaseDirectory, "..", "..", "..", "..", ".." }
            .Concat(parts)
            .ToArray();
        return Path.GetFullPath(Path.Combine(segments));
    }
}
