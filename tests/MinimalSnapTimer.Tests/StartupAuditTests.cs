namespace MinimalSnapTimer.Tests;

public sealed class StartupAuditTests
{
    [Fact]
    public void SettingsWindow_IsCreatedOnlyInOpenSettingsWindow()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "App.xaml.cs"));

        Assert.Equal(1, CountOccurrences(content, "new SettingsWindow"));
        Assert.Contains("private void OpenSettingsWindow()", content);
    }

    [Fact]
    public void SettingsWindow_UsesSafeOwnerFallback()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "App.xaml.cs"));
        var openSettingsBody = GetMethodBody(content, "private void OpenSettingsWindow()");

        Assert.DoesNotContain("Owner = _mainWindow", openSettingsBody);
        Assert.Contains("TryApplySettingsOwner(window);", openSettingsBody);
        Assert.Contains("WindowStartupLocation = WindowStartupLocation.CenterScreen", openSettingsBody);
        Assert.Contains("设置窗口 Owner 不可用", content);
    }

    [Fact]
    public void ThemeService_ReplacesThemeDictionary_InsteadOfAppendingInfiniteCopies()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Services", "ThemeService.cs"));

        Assert.Contains("var existing = merged.FirstOrDefault", content);
        Assert.Contains("merged[index] = replacement;", content);
        Assert.DoesNotContain("merged.Add(replacement);\r\n            merged.Add(replacement);", content);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
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
