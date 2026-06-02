using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;

namespace MinimalSnapTimer.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task SitCompletion_WaitsForStand_WhenAutoCycleDisabled()
    {
        var (vm, engine, settings, _, _) = CreateViewModel();
        settings.Timing.AutoCycleWorkflow = false;

        vm.StartSitCommand.Execute(null);
        engine.Advance(TimeSpan.FromMinutes(settings.Timing.SitMinutes));
        await Task.Delay(50);

        Assert.Equal(TimerState.Completed, vm.Snapshot.State);
        Assert.Equal("等待进入站立活动", vm.StatusText);
        Assert.True(vm.HasSuggestedNextStage);
    }

    [Fact]
    public async Task StandCompletion_WaitsForSit_WhenAutoCycleDisabled()
    {
        var (vm, engine, settings, _, _) = CreateViewModel();
        settings.Timing.AutoCycleWorkflow = false;

        vm.StartStandCommand.Execute(null);
        engine.Advance(TimeSpan.FromMinutes(settings.Timing.StandMinutes));
        await Task.Delay(50);

        Assert.Equal(TimerState.Completed, vm.Snapshot.State);
        Assert.Equal("等待进入坐着工作", vm.StatusText);
        Assert.True(vm.HasSuggestedNextStage);
    }

    [Fact]
    public async Task SitCompletion_AutoCyclesToStand_WhenEnabled()
    {
        var (vm, engine, settings, _, _) = CreateViewModel();
        settings.Timing.AutoCycleWorkflow = true;

        vm.StartSitCommand.Execute(null);
        engine.Advance(TimeSpan.FromMinutes(settings.Timing.SitMinutes));
        await Task.Delay(50);

        Assert.Equal(TimerState.Running, vm.Snapshot.State);
        Assert.Equal(WorkflowStage.Stand, vm.Snapshot.Stage);
        Assert.Equal(TimeSpan.FromMinutes(settings.Timing.StandMinutes), vm.Snapshot.CurrentTime);
    }

    [Fact]
    public async Task StandCompletion_AutoCyclesToSit_WhenEnabled()
    {
        var (vm, engine, settings, _, _) = CreateViewModel();
        settings.Timing.AutoCycleWorkflow = true;

        vm.StartStandCommand.Execute(null);
        engine.Advance(TimeSpan.FromMinutes(settings.Timing.StandMinutes));
        await Task.Delay(50);

        Assert.Equal(TimerState.Running, vm.Snapshot.State);
        Assert.Equal(WorkflowStage.Sit, vm.Snapshot.Stage);
        Assert.Equal(TimeSpan.FromMinutes(settings.Timing.SitMinutes), vm.Snapshot.CurrentTime);
    }

    [Fact]
    public void PureMode_CanToggle()
    {
        var (vm, _, _, _, _) = CreateViewModel();

        var initial = vm.IsPureMode;
        vm.TogglePureModeCommand.Execute(null);

        Assert.NotEqual(initial, vm.IsPureMode);
    }

    [Fact]
    public void SitQuickButtonText_UsesUpdatedPresetMinutes()
    {
        var (vm, _, settings, _, _) = CreateViewModel();
        settings.Presets.First(p => p.Type == PresetType.Sit).DurationMinutes = 1;

        vm.ApplySettings(settings);

        Assert.Equal("坐 1 分", vm.SitQuickButtonText);
    }

    [Fact]
    public void StandQuickButtonText_UsesUpdatedPresetMinutes()
    {
        var (vm, _, settings, _, _) = CreateViewModel();
        settings.Presets.First(p => p.Type == PresetType.Stand).DurationMinutes = 1;

        vm.ApplySettings(settings);

        Assert.Equal("站 1 分", vm.StandQuickButtonText);
    }

    [Fact]
    public void StartSitCommand_UsesUpdatedPresetMinutes()
    {
        var (vm, _, settings, _, _) = CreateViewModel();
        settings.Presets.First(p => p.Type == PresetType.Sit).DurationMinutes = 1;

        vm.ApplySettings(settings);
        vm.StartSitCommand.Execute(null);

        Assert.Equal(WorkflowStage.Sit, vm.Snapshot.Stage);
        Assert.Equal(TimeSpan.FromMinutes(1), vm.Snapshot.CurrentTime);
    }

    [Fact]
    public void StartStandCommand_UsesUpdatedPresetMinutes()
    {
        var (vm, _, settings, _, _) = CreateViewModel();
        settings.Presets.First(p => p.Type == PresetType.Stand).DurationMinutes = 1;

        vm.ApplySettings(settings);
        vm.StartStandCommand.Execute(null);

        Assert.Equal(WorkflowStage.Stand, vm.Snapshot.Stage);
        Assert.Equal(TimeSpan.FromMinutes(1), vm.Snapshot.CurrentTime);
    }

    [Fact]
    public void PureClickThrough_CanToggleState()
    {
        var (vm, _, _, _, _) = CreateViewModel();

        vm.SetPureClickThrough(true);
        Assert.True(vm.PureClickThrough);
        Assert.Equal("关闭点击穿透", vm.PureClickThroughMenuText);

        vm.SetPureClickThrough(false);
        Assert.False(vm.PureClickThrough);
        Assert.Equal("开启点击穿透", vm.PureClickThroughMenuText);
    }

    [Fact]
    public void TrayMenu_ProvidesDisableEntry_WhenClickThroughEnabled()
    {
        var context = CreateTrayContext(() => true);
        var labels = TrayService.GetTopLevelMenuTexts(context);

        Assert.Contains("关闭点击穿透", labels);
        Assert.Contains("当前：点击穿透已开启", labels);
    }

    [Fact]
    public void MainSurfaceMetadata_UpdatesWithCurrentMode()
    {
        var (vm, _, _, _, _) = CreateViewModel();

        Assert.Equal("标准窗口", vm.DisplayModeText);
        Assert.Equal("等待开始", vm.CurrentTaskText);

        vm.TogglePureModeCommand.Execute(null);

        Assert.Equal("纯时间模式", vm.DisplayModeText);
    }

    [Fact]
    public void LanguageSwitch_UpdatesClickThroughMenuText()
    {
        var localizer = new LocalizationService();
        var (vm, _, _, _, _) = CreateViewModel(localizer: localizer);

        vm.SetPureClickThrough(true);
        Assert.Equal("关闭点击穿透", vm.PureClickThroughMenuText);

        localizer.ApplyLanguage("en-US");

        Assert.Equal("Disable Click-Through", vm.PureClickThroughMenuText);
        Assert.Equal("Pause", vm.PauseResumeText);
    }

    [Fact]
    public void TrayMenu_UsesEnglishLabels_WhenLanguageIsEnglish()
    {
        var localizer = new LocalizationService();
        localizer.ApplyLanguage("en-US");
        var context = CreateTrayContext(() => true);

        var labels = TrayService.GetTopLevelMenuTexts(context, localizer);

        Assert.Contains("Disable Click-Through", labels);
        Assert.Contains("Current: click-through is enabled", labels);
        Assert.Contains("Show / Hide Main Window", labels);
    }

    [Fact]
    public void CanEnablePureClickThrough_IsFalse_WhenTrayUnavailable()
    {
        var (vm, _, _, _, _) = CreateViewModel();

        Assert.False(vm.CanEnablePureClickThrough());
    }

    [Fact]
    public void ApplySettings_KeepsPureClickThrough_WhenThemeChanges()
    {
        var (vm, _, settings, _, _) = CreateViewModel();
        settings.Appearance.PureClickThrough = true;
        settings.Appearance.Theme = ThemePreference.Dark;

        vm.ApplySettings(settings);

        Assert.True(vm.PureClickThrough);
        Assert.Equal(ThemePreference.Dark, settings.Appearance.Theme);
    }

    [Fact]
    public async Task Completion_InvokesNotificationService()
    {
        var notificationService = new FakeNotificationService();
        var (vm, engine, settings, _, _) = CreateViewModel(notificationService: notificationService);
        settings.Timing.AutoCycleWorkflow = false;

        vm.StartSitCommand.Execute(null);
        engine.Advance(TimeSpan.FromMinutes(settings.Timing.SitMinutes));
        await Task.Delay(50);

        Assert.Equal(1, notificationService.CallCount);
        Assert.Equal("坐着工作已结束", notificationService.Requests.Single().Title);
    }


    [Fact]
    public async Task CompletionFailure_IsCaptured_AndDoesNotBlockNextTimer()
    {
        var notificationService = new FakeNotificationService { ThrowOnShow = true };
        var errors = new List<string>();
        var (vm, engine, settings, _, _) = CreateViewModel(
            notificationService: notificationService,
            nonFatalErrorReporter: (message, ex) => errors.Add($"{message}|{ex?.GetType().Name}"));
        settings.Timing.AutoCycleWorkflow = false;

        vm.StartSitCommand.Execute(null);
        engine.Advance(TimeSpan.FromMinutes(settings.Timing.SitMinutes));
        await Task.Delay(50);

        vm.StartStandCommand.Execute(null);

        Assert.Single(errors);
        Assert.Contains("计时完成后的提醒链路发生异常", errors[0], StringComparison.Ordinal);
        Assert.Equal(WorkflowStage.Stand, vm.Snapshot.Stage);
        Assert.Equal(TimerState.Running, vm.Snapshot.State);
    }

    private static (MainViewModel ViewModel, TimerEngine Engine, AppSettings Settings, FakeNotificationService NotificationService, LocalizationService Localizer) CreateViewModel(
        LocalizationService? localizer = null,
        FakeNotificationService? notificationService = null,
        Action<string, Exception?>? nonFatalErrorReporter = null)
    {
        var settings = SettingsService.CreateDefaultSettings();
        settings.Reminder.ShowPopup = false;
        settings.Reminder.PlaySound = false;
        settings.Reminder.UseTrayBalloon = false;
        settings.General.LaunchInPureMode = false;
        settings.Appearance.EnablePureMode = false;

        localizer ??= new LocalizationService();
        notificationService ??= new FakeNotificationService();

        var timerEngine = new TimerEngine(new ManualTickSource());
        var presetManager = new PresetManager(settings);
        var taskbarService = new TaskbarService();
        var trayService = new TrayService(localizer);

        var viewModel = new MainViewModel(
            timerEngine,
            presetManager,
            notificationService,
            taskbarService,
            trayService,
            settings,
            localizer,
            nonFatalErrorReporter);

        return (viewModel, timerEngine, settings, notificationService, localizer);
    }

    private static TrayContext CreateTrayContext(Func<bool> isClickThrough)
    {
        return new TrayContext
        {
            GetCurrentStatus = () => "运行中",
            GetCurrentTimeText = () => "00:39:58",
            IsMainWindowVisible = () => true,
            ToggleMainWindowVisibility = () => { },
            TogglePureMode = () => { },
            ShowPureWindow = () => { },
            ReturnToStandardMode = () => { },
            StartSit = () => { },
            StartStand = () => { },
            StartPresetById = _ => { },
            GetPresets = () => [],
            IsPaused = () => false,
            PauseOrResume = () => { },
            Stop = () => { },
            Reset = () => { },
            IsAlwaysOnTop = () => false,
            ToggleAlwaysOnTop = () => { },
            IsPureClickThrough = isClickThrough,
            SetPureClickThrough = _ => { },
            OpenSettings = () => { },
            Exit = () => { }
        };
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<CompletionNotificationRequest> Requests { get; } = [];

        public int CallCount => Requests.Count;

        public bool ThrowOnShow { get; init; }

        public Task<ReminderResult> ShowCompletionAsync(CompletionNotificationRequest request)
        {
            Requests.Add(request);
            if (ThrowOnShow)
            {
                throw new InvalidOperationException("reminder failed");
            }

            return Task.FromResult(new ReminderResult { Action = ReminderAction.None });
        }

        public void StopLoopingSound()
        {
        }
    }
}

