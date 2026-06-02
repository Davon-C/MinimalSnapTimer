using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;

namespace MinimalSnapTimer.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void VersionText_UsesSupportServiceVersion()
    {
        var settings = SettingsService.CreateDefaultSettings();
        var supportService = new FakeAppSupportService { AppVersionValue = "0.1.0-beta" };
        var viewModel = new SettingsViewModel(settings, LocalizationService.Instance, supportService);

        Assert.Equal("MinimalSnapTimer v0.1.0-beta", viewModel.VersionText);
    }

    [Fact]
    public void OpenLogDirectoryCommand_DoesNotThrow_WhenPathExists()
    {
        var settings = SettingsService.CreateDefaultSettings();
        var supportService = new FakeAppSupportService();
        var viewModel = new SettingsViewModel(settings, LocalizationService.Instance, supportService);

        var exception = Record.Exception(() => viewModel.OpenLogDirectoryCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(supportService.LogOpened);
    }

    [Fact]
    public void OpenConfigDirectoryCommand_DoesNotThrow_WhenPathExists()
    {
        var settings = SettingsService.CreateDefaultSettings();
        var supportService = new FakeAppSupportService();
        var viewModel = new SettingsViewModel(settings, LocalizationService.Instance, supportService);

        var exception = Record.Exception(() => viewModel.OpenConfigDirectoryCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(supportService.ConfigOpened);
    }

    [Fact]
    public void ResetWindowPositionsCommand_RestoresDefaults()
    {
        var settings = SettingsService.CreateDefaultSettings();
        settings.Windows.MainLeft = 999;
        settings.Windows.PureTop = 888;
        var supportService = new FakeAppSupportService();
        var viewModel = new SettingsViewModel(settings, LocalizationService.Instance, supportService);

        viewModel.ResetWindowPositionsCommand.Execute(null);

        Assert.Equal(120, settings.Windows.MainLeft);
        Assert.Equal(180, settings.Windows.PureTop);
        Assert.True(supportService.ResetCalled);
    }

    private sealed class FakeAppSupportService : IAppSupportService
    {
        public string AppVersionValue { get; set; } = "0.1.0-test";

        public bool LogOpened { get; private set; }

        public bool ConfigOpened { get; private set; }

        public bool ResetCalled { get; private set; }

        public string AppVersion => AppVersionValue;

        public string LogDirectory => Path.Combine(Path.GetTempPath(), "MinimalSnapTimerLogs");

        public string ConfigDirectory => Path.Combine(Path.GetTempPath(), "MinimalSnapTimerConfig");

        public string ExecutablePath => Path.Combine(Path.GetTempPath(), "MinimalSnapTimer.exe");

        public void OpenLogDirectory()
        {
            Directory.CreateDirectory(LogDirectory);
            LogOpened = true;
        }

        public void OpenConfigDirectory()
        {
            Directory.CreateDirectory(ConfigDirectory);
            ConfigOpened = true;
        }

        public void ResetWindowPlacements(AppSettings settings)
        {
            settings.Windows = new WindowPlacementSettings();
            ResetCalled = true;
        }
    }
}
