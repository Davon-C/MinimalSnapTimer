namespace MinimalSnapTimer.Tests;

public sealed class ThemeAssetTests
{
    [Fact]
    public void BaseTheme_ContainsComboBoxPopupAndToggleTemplateParts()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Themes", "BaseTheme.xaml"));

        Assert.Contains("TargetType=\"ComboBox\"", content);
        Assert.Contains("PART_Popup", content);
        Assert.Contains("DropDownToggle", content);
        Assert.Contains("IsChecked=\"{Binding IsDropDownOpen", content);
    }

    [Fact]
    public void BaseTheme_ContainsCardAndBadgeStyles()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Themes", "BaseTheme.xaml"));

        Assert.Contains("CardBorderStyle", content);
        Assert.Contains("BadgeBorderStyle", content);
        Assert.Contains("HeroTimeTextBlockStyle", content);
        Assert.Contains("FlatSecondaryButtonStyle", content);
    }

    [Fact]
    public void AppIcon_ResourceFilesExist()
    {
        Assert.True(File.Exists(GetSourcePath("src", "MinimalSnapTimer", "Assets", "AppIcon.ico")));
        Assert.True(File.Exists(GetSourcePath("src", "MinimalSnapTimer", "Assets", "AppIcon.png")));
    }

    [Fact]
    public void ProjectFile_ContainsApplicationIconConfiguration()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "MinimalSnapTimer.csproj"));

        Assert.Contains("<ApplicationIcon>Assets\\AppIcon.ico</ApplicationIcon>", content);
    }

    [Fact]
    public void SettingsWindow_ReadOnlyDisplayBindings_UseOneWayMode()
    {
        var content = File.ReadAllText(GetSourcePath("src", "MinimalSnapTimer", "Views", "SettingsWindow.xaml"));

        Assert.Contains("Text=\"{Binding VersionText, Mode=OneWay}\"", content);
        Assert.Contains("Text=\"{Binding SettingsVersion, Mode=OneWay}\"", content);
        Assert.Contains("Text=\"{Binding LogDirectoryPath, Mode=OneWay}\"", content);
        Assert.Contains("Text=\"{Binding ConfigDirectoryPath, Mode=OneWay}\"", content);
        Assert.Contains("Text=\"{Binding ExecutablePath, Mode=OneWay}\"", content);
    }

    private static string GetSourcePath(params string[] parts)
    {
        var segments = new[] { AppContext.BaseDirectory, "..", "..", "..", "..", ".." }
            .Concat(parts)
            .ToArray();
        return Path.GetFullPath(Path.Combine(segments));
    }
}
