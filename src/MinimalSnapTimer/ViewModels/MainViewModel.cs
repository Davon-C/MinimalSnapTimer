using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using MinimalSnapTimer.Infrastructure;
using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MinimalSnapTimer.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly TimerEngine _timerEngine;
    private readonly PresetManager _presetManager;
    private readonly INotificationService _notificationService;
    private readonly TaskbarService _taskbarService;
    private readonly TrayService _trayService;
    private readonly ILocalizationService _localizer;
    private readonly Action<string, Exception?> _nonFatalErrorReporter;
    private AppSettings _settings;
    private string _inputDuration = "40";
    private string _displayTime = "00:40:00";
    private string _statusText = "\u672a\u5f00\u59cb";
    private string _windowTitle = "MinimalSnapTimer";
    private string _pauseResumeText = "\u6682\u505c";
    private bool _isPureMode;
    private bool _isAlwaysOnTop;
    private bool _showInTaskbar = true;
    private double _windowOpacity = 1.0d;
    private Brush _pureForeground = Brushes.White;
    private Brush _pureBackground = Brushes.Black;
    private double _pureBackgroundOpacity = 0.55d;
    private bool _pureClickThrough;
    private WorkflowStage _pendingSuggestedStage = WorkflowStage.None;
    private WorkflowStage _lastCompletedStage = WorkflowStage.None;
    private TimerPreset? _currentPreset;

    public MainViewModel(
        TimerEngine timerEngine,
        PresetManager presetManager,
        INotificationService notificationService,
        TaskbarService taskbarService,
        TrayService trayService,
        AppSettings settings,
        ILocalizationService? localizer = null,
        Action<string, Exception?>? nonFatalErrorReporter = null)
    {
        _timerEngine = timerEngine;
        _presetManager = presetManager;
        _notificationService = notificationService;
        _taskbarService = taskbarService;
        _trayService = trayService;
        _localizer = localizer ?? LocalizationService.Instance;
        _nonFatalErrorReporter = nonFatalErrorReporter ?? ((_, _) => { });
        _settings = settings;

        Presets = new ObservableCollection<TimerPreset>(_presetManager.Presets);
        StartCommand = new RelayCommand(StartFromInput);
        PauseResumeCommand = new RelayCommand(() => _timerEngine.TogglePauseResume());
        StopCommand = new RelayCommand(Stop);
        ResetCommand = new RelayCommand(Reset);
        StartSitCommand = new RelayCommand(() => StartWorkflow(WorkflowStage.Sit));
        StartStandCommand = new RelayCommand(() => StartWorkflow(WorkflowStage.Stand));
        TogglePureModeCommand = new RelayCommand(TogglePureMode);
        ToggleAlwaysOnTopCommand = new RelayCommand(ToggleAlwaysOnTop);
        OpenSettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        HideToTrayCommand = new RelayCommand(() => HideToTrayRequested?.Invoke(this, EventArgs.Empty));
        ActivateSuggestedStageCommand = new RelayCommand(ActivateSuggestedStage, () => _pendingSuggestedStage != WorkflowStage.None);

        _timerEngine.SnapshotChanged += (_, snapshot) => UpdateFromSnapshot(snapshot);
        _timerEngine.Completed += (_, args) => _ = HandleCompletionSafelyAsync(args.Snapshot);
        _localizer.PropertyChanged += OnLocalizerPropertyChanged;

        ApplySettings(_settings);
    }

    public event EventHandler? OpenSettingsRequested;

    public event EventHandler<bool>? PureModeChangeRequested;

    public event EventHandler? HideToTrayRequested;

    public event EventHandler? SettingsPersistenceRequested;

    public ObservableCollection<TimerPreset> Presets { get; }

    public RelayCommand StartCommand { get; }

    public RelayCommand PauseResumeCommand { get; }

    public RelayCommand StopCommand { get; }

    public RelayCommand ResetCommand { get; }

    public RelayCommand StartSitCommand { get; }

    public RelayCommand StartStandCommand { get; }

    public RelayCommand TogglePureModeCommand { get; }

    public RelayCommand ToggleAlwaysOnTopCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public RelayCommand HideToTrayCommand { get; }

    public RelayCommand ActivateSuggestedStageCommand { get; }

    public string InputDuration
    {
        get => _inputDuration;
        set => SetProperty(ref _inputDuration, value);
    }

    public string DisplayTime
    {
        get => _displayTime;
        private set => SetProperty(ref _displayTime, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        private set => SetProperty(ref _windowTitle, value);
    }

    public string PauseResumeText
    {
        get => _pauseResumeText;
        private set => SetProperty(ref _pauseResumeText, value);
    }

    public string SitQuickButtonText => $"{_localizer["main.quick.sitPrefix"]} {GetWorkflowMinutes(WorkflowStage.Sit)} {_localizer["main.minutes"]}";

    public string StandQuickButtonText => $"{_localizer["main.quick.standPrefix"]} {GetWorkflowMinutes(WorkflowStage.Stand)} {_localizer["main.minutes"]}";

    public string PureClickThroughMenuText => PureClickThrough ? _localizer["pure.disableClickThrough"] : _localizer["pure.enableClickThrough"];

    public string DisplayModeText => IsPureMode
        ? _localizer["main.surface.pureModeOn"]
        : _localizer["main.surface.standardModeOn"];

    public string AlertStatusText => GetAlertStatusText();

    public string CurrentTaskText => GetCurrentTaskText(Snapshot);

    public string ModeToggleText => IsPureMode
        ? _localizer["main.standardMode"]
        : _localizer["main.pureMode"];

    public bool IsPureMode
    {
        get => _isPureMode;
        private set => SetProperty(ref _isPureMode, value);
    }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set
        {
            if (SetProperty(ref _isAlwaysOnTop, value))
            {
                _settings.Appearance.AlwaysOnTop = value;
                SettingsPersistenceRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool ShowInTaskbar
    {
        get => _showInTaskbar;
        private set => SetProperty(ref _showInTaskbar, value);
    }

    public double WindowOpacityValue
    {
        get => _windowOpacity;
        private set => SetProperty(ref _windowOpacity, value);
    }

    public Brush PureForeground
    {
        get => _pureForeground;
        private set => SetProperty(ref _pureForeground, value);
    }

    public Brush PureBackground
    {
        get => _pureBackground;
        private set => SetProperty(ref _pureBackground, value);
    }

    public double PureBackgroundOpacity
    {
        get => _pureBackgroundOpacity;
        private set => SetProperty(ref _pureBackgroundOpacity, value);
    }

    public bool PureClickThrough
    {
        get => _pureClickThrough;
        private set => SetProperty(ref _pureClickThrough, value);
    }

    public TimerSnapshot Snapshot => _timerEngine.Snapshot;

    public AppSettings Settings => _settings;

    public bool IsPaused => Snapshot.State == TimerState.Paused;

    public bool HasSuggestedNextStage => _pendingSuggestedStage != WorkflowStage.None;

    public bool CanEnablePureClickThrough() => _trayService.IsAvailable;

    public void NotifySettingsChanged()
    {
        SettingsPersistenceRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetPureClickThrough(bool enabled)
    {
        if (PureClickThrough == enabled)
        {
            return;
        }

        PureClickThrough = enabled;
        _settings.Appearance.PureClickThrough = enabled;
        RefreshTrayState();
        _trayService.RebuildMenu();
        OnPropertyChanged(nameof(PureClickThroughMenuText));
        SettingsPersistenceRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        SettingsService.EnsureDefaults(_settings);
        RefreshPresetCollection();
        InputDuration = _settings.Timing.DefaultMinutes.ToString(CultureInfo.InvariantCulture);
        IsAlwaysOnTop = _settings.Appearance.AlwaysOnTop;
        IsPureMode = _settings.Appearance.EnablePureMode || _settings.General.LaunchInPureMode;
        ShowInTaskbar = !_settings.Appearance.HideTaskbarButton;
        WindowOpacityValue = Math.Clamp(_settings.Appearance.WindowOpacity, 0.3d, 1d);
        PureBackgroundOpacity = Math.Clamp(_settings.Appearance.PureBackgroundOpacity, 0.3d, 1d);
        PureForeground = ParseBrush(_settings.Appearance.PureFontColor, Brushes.White);
        PureBackground = ParseBrush(_settings.Appearance.PureBackgroundColor, Brushes.Black);
        PureClickThrough = _settings.Appearance.PureClickThrough;
        OnPropertyChanged(nameof(SitQuickButtonText));
        OnPropertyChanged(nameof(StandQuickButtonText));
        OnPropertyChanged(nameof(PureClickThroughMenuText));
        OnPropertyChanged(nameof(DisplayModeText));
        OnPropertyChanged(nameof(AlertStatusText));
        OnPropertyChanged(nameof(CurrentTaskText));
        OnPropertyChanged(nameof(ModeToggleText));
        UpdateFromSnapshot(_timerEngine.Snapshot);
    }

    public void ApplyCommandLine(CommandLineOptions options)
    {
        if (options.AlwaysOnTop)
        {
            IsAlwaysOnTop = true;
        }

        if (options.MinimizeToTray)
        {
            HideToTrayRequested?.Invoke(this, EventArgs.Empty);
        }

        if (options.SoundOff)
        {
            _settings.Reminder.PlaySound = false;
        }

        if (options.AutoRestart)
        {
            _settings.Timing.AutoRestart = true;
        }

        if (options.StartInPureMode)
        {
            IsPureMode = true;
            PureModeChangeRequested?.Invoke(this, true);
        }

        if (!string.IsNullOrWhiteSpace(options.PresetName))
        {
            var preset = _presetManager.GetByName(options.PresetName!);
            if (preset is not null)
            {
                StartPreset(preset, options.StartPaused);
                return;
            }
        }

        if (options.Mode is WorkflowStage mode)
        {
            StartWorkflow(mode, options.StartPaused);
            return;
        }

        if (options.Duration is TimeSpan duration)
        {
            StartTimer(duration, WorkflowStage.None, null, options.StartPaused);
        }
    }

    public void StartPresetById(string presetId)
    {
        var preset = _presetManager.GetById(presetId);
        if (preset is not null)
        {
            StartPreset(preset);
        }
    }

    public void HandleHotkeyAction(string action)
    {
        switch (action)
        {
            case "StartPauseResume":
                if (Snapshot.State == TimerState.Idle || Snapshot.State == TimerState.Completed)
                {
                    StartFromInput();
                }
                else
                {
                    _timerEngine.TogglePauseResume();
                }

                break;
            case "Reset":
                Reset();
                break;
            case "Stop":
                Stop();
                break;
            case "ToggleTopmost":
                ToggleAlwaysOnTop();
                break;
            case "MinimizeToTray":
            case "HideWindow":
                HideToTrayRequested?.Invoke(this, EventArgs.Empty);
                break;
            case "TogglePureMode":
                TogglePureMode();
                break;
            case "StartSit":
                StartWorkflow(WorkflowStage.Sit);
                break;
            case "StartStand":
                StartWorkflow(WorkflowStage.Stand);
                break;
        }
    }

    private void StartFromInput()
    {
        if (!TryParseInputDuration(InputDuration, out var duration))
        {
            StatusText = _localizer["main.status.invalidTime"];
            return;
        }

        StartTimer(duration, WorkflowStage.None, null, false);
    }

    private void StartWorkflow(WorkflowStage stage, bool startPaused = false)
    {
        var preset = _presetManager.GetWorkflowPreset(stage);
        var minutes = GetWorkflowMinutes(stage);
        StartTimer(TimeSpan.FromMinutes(minutes), stage, preset, startPaused);
    }

    private void StartPreset(TimerPreset preset, bool startPaused = false)
    {
        var stage = preset.Type switch
        {
            PresetType.Sit => WorkflowStage.Sit,
            PresetType.Stand => WorkflowStage.Stand,
            PresetType.Break => WorkflowStage.Break,
            PresetType.Focus => WorkflowStage.Focus,
            PresetType.Custom => WorkflowStage.Custom,
            _ => WorkflowStage.None
        };

        _currentPreset = preset;
        StartTimer(TimeSpan.FromMinutes(preset.DurationMinutes), stage, preset, startPaused);
    }

    private void StartTimer(TimeSpan duration, WorkflowStage stage, TimerPreset? preset, bool startPaused)
    {
        _currentPreset = preset;
        _pendingSuggestedStage = WorkflowStage.None;
        _settings.Timing.RecentTimer = duration == TimeSpan.Zero ? "00:00:00" : duration.ToString(@"hh\:mm\:ss");
        _timerEngine.Start(duration, stage, preset?.Id, startPaused);
        SettingsPersistenceRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Stop()
    {
        _pendingSuggestedStage = WorkflowStage.None;
        _currentPreset = null;
        _timerEngine.Stop();
    }

    private void Reset()
    {
        _pendingSuggestedStage = WorkflowStage.None;
        _timerEngine.Reset();
    }

    private void TogglePureMode()
    {
        IsPureMode = !IsPureMode;
        _settings.Appearance.EnablePureMode = IsPureMode;
        PureModeChangeRequested?.Invoke(this, IsPureMode);
        OnPropertyChanged(nameof(DisplayModeText));
        OnPropertyChanged(nameof(ModeToggleText));
        SettingsPersistenceRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
    }

    private void ActivateSuggestedStage()
    {
        if (_pendingSuggestedStage == WorkflowStage.None)
        {
            return;
        }

        var stage = _pendingSuggestedStage;
        _pendingSuggestedStage = WorkflowStage.None;
        ActivateSuggestedStageCommand.RaiseCanExecuteChanged();
        StartWorkflow(stage);
    }

    private async Task HandleCompletionSafelyAsync(TimerSnapshot snapshot)
    {
        try
        {
            await HandleCompletionAsync(snapshot);
        }
        catch (Exception ex)
        {
            _nonFatalErrorReporter("计时完成后的提醒链路发生异常，已按非致命错误记录。", ex);
        }
    }

    private async Task HandleCompletionAsync(TimerSnapshot snapshot)
    {
        _lastCompletedStage = snapshot.Stage;

        if (_settings.Timing.AutoRestart && snapshot.Mode == TimerMode.Countdown)
        {
            StartTimer(snapshot.InitialDuration, snapshot.Stage, _currentPreset, false);
            return;
        }

        if (_currentPreset is not null && _currentPreset.AutoSwitchToNext && !string.IsNullOrWhiteSpace(_currentPreset.NextPresetId))
        {
            var nextPreset = _presetManager.GetById(_currentPreset.NextPresetId!);
            if (nextPreset is not null)
            {
                StartPreset(nextPreset);
                return;
            }
        }

        if (_settings.Timing.AutoCycleWorkflow && snapshot.Stage == WorkflowStage.Sit)
        {
            StartWorkflow(WorkflowStage.Stand);
            return;
        }

        if (_settings.Timing.AutoCycleWorkflow && snapshot.Stage == WorkflowStage.Stand)
        {
            StartWorkflow(WorkflowStage.Sit);
            return;
        }

        _pendingSuggestedStage = GetSuggestedNextStage(snapshot.Stage);
        ActivateSuggestedStageCommand.RaiseCanExecuteChanged();
        UpdateFromSnapshot(snapshot);

        var request = BuildCompletionRequest(snapshot.Stage);
        var result = await _notificationService.ShowCompletionAsync(request);

        switch (result.Action)
        {
            case ReminderAction.StartSuggestedStage:
                ActivateSuggestedStage();
                break;
            case ReminderAction.RepeatCurrentStage:
                if (_lastCompletedStage != WorkflowStage.None)
                {
                    StartWorkflow(_lastCompletedStage);
                }

                break;
            case ReminderAction.Snooze:
                StartTimer(TimeSpan.FromMinutes(_settings.Reminder.SnoozeMinutes), _lastCompletedStage, null, false);
                break;
            case ReminderAction.Stop:
                Stop();
                break;
        }
    }

    private CompletionNotificationRequest BuildCompletionRequest(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.Sit => new CompletionNotificationRequest
            {
                Title = _localizer["notification.sitTitle"],
                Message = _localizer["notification.sitMessage"],
                SuggestedNextStage = WorkflowStage.Stand,
                Settings = _settings
            },
            WorkflowStage.Stand => new CompletionNotificationRequest
            {
                Title = _localizer["notification.standTitle"],
                Message = _localizer["notification.standMessage"],
                SuggestedNextStage = WorkflowStage.Sit,
                Settings = _settings
            },
            _ => new CompletionNotificationRequest
            {
                Title = _localizer["notification.timerTitle"],
                Message = _localizer["notification.timerMessage"],
                SuggestedNextStage = WorkflowStage.None,
                Settings = _settings
            }
        };
    }

    private void UpdateFromSnapshot(TimerSnapshot snapshot)
    {
        DisplayTime = snapshot.Mode == TimerMode.Stopwatch
            ? snapshot.CurrentTime.ToString(@"hh\:mm\:ss")
            : snapshot.CurrentTime.ToString(_settings.Timing.ShowSeconds ? @"hh\:mm\:ss" : @"hh\:mm");

        PauseResumeText = snapshot.State == TimerState.Paused ? _localizer["main.resume"] : _localizer["main.pause"];
        StatusText = GetStatusText(snapshot);
        WindowTitle = BuildWindowTitle(snapshot);
        RefreshTrayState();
        _taskbarService.Update(snapshot, WindowTitle);
        OnPropertyChanged(nameof(Snapshot));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(CurrentTaskText));
        OnPropertyChanged(nameof(AlertStatusText));
    }

    private string GetStatusText(TimerSnapshot snapshot)
    {
        if (snapshot.State == TimerState.Completed && _pendingSuggestedStage != WorkflowStage.None)
        {
            return snapshot.Stage == WorkflowStage.Sit
                ? _localizer["main.status.waitStand"]
                : _localizer["main.status.waitSit"];
        }

        return snapshot switch
        {
            { Mode: TimerMode.Stopwatch, State: TimerState.Running or TimerState.Paused } => _localizer["main.status.stopwatch"],
            { State: TimerState.Running, Stage: WorkflowStage.Sit } => _localizer["main.status.sitRunning"],
            { State: TimerState.Running, Stage: WorkflowStage.Stand } => _localizer["main.status.standRunning"],
            { State: TimerState.Running } => _localizer["main.status.running"],
            { State: TimerState.Paused } => _localizer["main.status.paused"],
            { State: TimerState.Completed } => _localizer["main.status.completed"],
            _ => _localizer["main.status.idle"]
        };
    }

    private string BuildWindowTitle(TimerSnapshot snapshot)
    {
        if (snapshot.State == TimerState.Idle)
        {
            return "MinimalSnapTimer";
        }

        var prefix = snapshot.Stage switch
        {
            WorkflowStage.Sit => $"{_localizer["main.quick.sitPrefix"]} ",
            WorkflowStage.Stand => $"{_localizer["main.quick.standPrefix"]} ",
            _ => string.Empty
        };

        return $"[{prefix}{DisplayTime}] MinimalSnapTimer";
    }

    private void RefreshPresetCollection()
    {
        Presets.Clear();
        foreach (var preset in _presetManager.Presets)
        {
            Presets.Add(preset);
        }

        OnPropertyChanged(nameof(SitQuickButtonText));
        OnPropertyChanged(nameof(StandQuickButtonText));
    }

    private int GetWorkflowMinutes(WorkflowStage stage)
    {
        var preset = _presetManager.GetWorkflowPreset(stage);
        if (preset is not null)
        {
            return preset.DurationMinutes;
        }

        return stage switch
        {
            WorkflowStage.Sit => _settings.Timing.SitMinutes,
            WorkflowStage.Stand => _settings.Timing.StandMinutes,
            _ => _settings.Timing.DefaultMinutes
        };
    }

    private void RefreshTrayState()
    {
        var status = PureClickThrough
            ? $"{_localizer["pure.clickThroughTooltip"]} {StatusText}"
            : StatusText;

        _trayService.UpdateTooltip(status, DisplayTime);
        _trayService.SetStateIcon(Snapshot.State);
        _trayService.RebuildMenu();
    }

    private string GetAlertStatusText()
    {
        var enabledAlerts = new List<string>();

        if (_settings.Reminder.UseToastNotifications)
        {
            enabledAlerts.Add(_localizer["main.surface.channel.toast"]);
        }

        if (_settings.Reminder.ShowPopup)
        {
            enabledAlerts.Add(_localizer["main.surface.channel.popup"]);
        }

        if (_settings.Reminder.UseTrayBalloon)
        {
            enabledAlerts.Add(_localizer["main.surface.channel.tray"]);
        }

        return enabledAlerts.Count == 0
            ? _localizer["main.surface.alertsOff"]
            : string.Join(" / ", enabledAlerts);
    }

    private string GetCurrentTaskText(TimerSnapshot snapshot)
    {
        if (snapshot.Mode == TimerMode.Stopwatch)
        {
            return _localizer["main.surface.stopwatchMode"];
        }

        return snapshot.Stage switch
        {
            WorkflowStage.Sit => _localizer["main.surface.sitTask"],
            WorkflowStage.Stand => _localizer["main.surface.standTask"],
            WorkflowStage.Break => _localizer["main.surface.breakTask"],
            WorkflowStage.Focus => _localizer["main.surface.focusTask"],
            WorkflowStage.Custom => _localizer["main.surface.customTask"],
            _ => snapshot.State == TimerState.Idle
                ? _localizer["main.surface.noneTask"]
                : _localizer["main.surface.timerMode"]
        };
    }

    private static WorkflowStage GetSuggestedNextStage(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.Sit => WorkflowStage.Stand,
            WorkflowStage.Stand => WorkflowStage.Sit,
            _ => WorkflowStage.None
        };
    }

    private static bool TryParseInputDuration(string input, out TimeSpan duration)
    {
        if (int.TryParse(input, out var minutes))
        {
            duration = TimeSpan.FromMinutes(minutes);
            return true;
        }

        return TimeSpan.TryParse(input, out duration);
    }

    private static Brush ParseBrush(string value, Brush fallback)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(value)!;
        }
        catch
        {
            return fallback;
        }
    }

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(LocalizationService.CurrentLanguage) and not "Item[]")
        {
            return;
        }

        OnPropertyChanged(nameof(SitQuickButtonText));
        OnPropertyChanged(nameof(StandQuickButtonText));
        OnPropertyChanged(nameof(PureClickThroughMenuText));
        OnPropertyChanged(nameof(DisplayModeText));
        OnPropertyChanged(nameof(AlertStatusText));
        OnPropertyChanged(nameof(CurrentTaskText));
        OnPropertyChanged(nameof(ModeToggleText));
        UpdateFromSnapshot(_timerEngine.Snapshot);
    }
}
