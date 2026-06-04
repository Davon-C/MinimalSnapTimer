namespace MinimalSnapTimer.Tests;

public sealed class TrayServiceImplementationTests
{
    [Fact]
    public void TrayService_RebuildMenuAndTooltip_EnsureTrayIcon()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Services", "TrayService.cs"));

        Assert.Contains("public void RebuildMenu()", content);
        Assert.Contains("public void UpdateStateSummary()", content);
        Assert.Contains("public void UpdateTooltip(string status, string timeText)", content);
        Assert.Contains("public void SetStateIcon(TimerState state)", content);
        Assert.Contains("public void EnsureTrayVisible()", content);
        Assert.Contains("EnsureTrayIcon();", content);
        Assert.Contains("tray.currentStatus", content);
        Assert.Contains("tray.currentTime", content);
    }

    [Fact]
    public void TrayService_UpdateStateSummary_DoesNotRebuildContextMenu()
    {
        var trayContent = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Services", "TrayService.cs"));
        var viewModelContent = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("public void UpdateStateSummary()", trayContent);
        Assert.DoesNotContain("RebuildMenu();", GetMethodBody(viewModelContent, "private void RefreshTrayState()"));
        Assert.Contains("_trayService.UpdateStateSummary();", viewModelContent);
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

    private static string GetMethodBody(string content, string signature)
    {
        var start = content.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Method signature not found: {signature}");

        var braceStart = content.IndexOf('{', start);
        Assert.True(braceStart >= 0, $"Method body start not found: {signature}");

        var depth = 0;
        for (var i = braceStart; i < content.Length; i++)
        {
            if (content[i] == '{')
            {
                depth++;
            }
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return content.Substring(braceStart, i - braceStart + 1);
                }
            }
        }

        throw new InvalidOperationException($"Method body end not found: {signature}");
    }
}
