using System.Windows;
using System.Windows.Interop;
using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;
using MinimalSnapTimer.Views;

namespace MinimalSnapTimer;

public partial class App : System.Windows.Application
{
    private readonly LoggingService _loggingService = new();
    private readonly StartupDiagnosticsService _startupDiagnostics = new();
    private readonly ILocalizationService _localizer = LocalizationService.Instance;
    private IAppSupportService? _appSupportService;
    private SettingsService? _settingsService;
    private SingleInstanceService? _singleInstanceService;
    private CancellationTokenSource? _singleInstanceCts;
    private TrayService? _trayService;
    private TaskbarService? _taskbarService;
    private NotificationService? _notificationService;
    private HotkeyService? _hotkeyService;
    private AutoStartService? _autoStartService;
    private PresetManager? _presetManager;
    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;
    private PureTimerWindow? _pureWindow;
    private AppSettings _settings = new();
    private bool _isExiting;
    private bool _startupCompleted;
    private bool _fatalDialogOpen;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _startupDiagnostics.WriteEnvironmentSnapshot();
        RegisterGlobalExceptionHandlers();

        try
        {
            _settingsService = new SettingsService(AppContext.BaseDirectory);
            _appSupportService = new AppSupportService(_settingsService, _startupDiagnostics);
            _startupDiagnostics.Write($"设置文件路径: {_settingsService.SettingsPath}");

            _settings = await _settingsService.LoadAsync();
            SettingsService.EnsureDefaults(_settings);
            _startupDiagnostics.Write("设置加载成功。");

            ApplyLocalizationSafe();
            ApplyThemeSafe();

            if (_settings.General.SingleInstance)
            {
                _startupDiagnostics.Write("单实例模式已启用，开始检查已有实例。");
                _singleInstanceService = new SingleInstanceService("MinimalSnapTimer");
                if (!_singleInstanceService.TryAcquire())
                {
                    _startupDiagnostics.Write("检测到已有实例，尝试转发命令行参数。");
                    if (await _singleInstanceService.ForwardToPrimaryAsync(e.Args))
                    {
                        Shutdown();
                        return;
                    }

                    _startupDiagnostics.Write("命令转发失败，疑似已有实例无响应。本次将继续启动新的可见实例。");
                    _singleInstanceService.Dispose();
                    _singleInstanceService = null;
                }
            }

            _trayService = new TrayService(_localizer, new IconService(), (message, ex) => _startupDiagnostics.Write(message, ex));
            _taskbarService = new TaskbarService();
            _notificationService = new NotificationService(localizer: _localizer);
            _autoStartService = new AutoStartService();
            _hotkeyService = new HotkeyService();

            _startupDiagnostics.Write($"通知开关: {_settings.Reminder.UseToastNotifications}");
            _startupDiagnostics.Write($"当前语言: {_settings.General.Language}");
            _startupDiagnostics.Write($"当前主题: {_settings.Appearance.Theme}");

            var tickSource = new DispatcherTickSource();
            var timerEngine = new TimerEngine(tickSource);
            _presetManager = new PresetManager(_settings);

            _mainViewModel = new MainViewModel(
                timerEngine,
                _presetManager,
                _notificationService,
                _taskbarService,
                _trayService,
                _settings,
                _localizer,
                (message, ex) => _startupDiagnostics.Write(message, ex));

            _mainViewModel.OpenSettingsRequested += (_, _) => OpenSettingsWindow();
            _mainViewModel.PureModeChangeRequested += (_, pureMode) => SwitchMode(pureMode);
            _mainViewModel.HideToTrayRequested += (_, _) => HideToTray();
            _mainViewModel.SettingsPersistenceRequested += async (_, _) => await PersistSettingsAsync();

            _mainWindow = new MainWindow { DataContext = _mainViewModel };
            _pureWindow = new PureTimerWindow { DataContext = _mainViewModel };
            _startupDiagnostics.Write("主窗口和纯时间窗口已创建。");

            _notificationService.ReminderDialogFactory = ShowReminderDialogAsync;
            _notificationService.TrayBalloonCallback = (title, message) =>
            {
                try
                {
                    _trayService?.ShowBalloon(title, message);
                }
                catch (Exception ex)
                {
                    _startupDiagnostics.Write("托盘气泡提醒发送失败。", ex);
                }
            };
            _notificationService.NotificationFailureCallback = message =>
            {
                try
                {
                    _trayService?.ShowBalloon(_localizer["reminder.title"], message);
                }
                catch (Exception ex)
                {
                    _startupDiagnostics.Write("通知降级为托盘提醒时失败。", ex);
                }
            };
            _startupDiagnostics.Write("通知服务初始化成功。");

            _mainWindow.Closing += OnMainWindowClosing;
            _pureWindow.Closing += OnPureWindowClosing;
            _mainWindow.Loaded += (_, _) => RegisterGlobalHotkeysIfNeeded();
            _pureWindow.Loaded += (_, _) => ApplyPureWindowSettings();

            ApplyWindowPlacement();
            _startupDiagnostics.Write("窗口位置已校正。");

            TryAttachTaskbar();
            var trayAvailable = TryInitializeTray();
            ApplyAutoStartSetting();

            var commandLineService = new CommandLineService();
            var options = commandLineService.Parse(e.Args);
            if (!options.IsValid || options.ShowHelp)
            {
                System.Windows.MessageBox.Show(
                    options.IsValid ? commandLineService.GetHelpText() : $"{options.ValidationError}\n\n{commandLineService.GetHelpText()}",
                    _localizer["app.name"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            if (options.IsValid)
            {
                _mainViewModel.ApplyCommandLine(options);
            }

            if (options.IsValid && e.Args.Length == 0 && _settings.General.AutoStartOnLaunch)
            {
                _mainViewModel.StartCommand.Execute(null);
            }

            ShowStartupWindows(options, trayAvailable);
            EnsureVisibleStartupSurface(trayAvailable);

            if (_singleInstanceService is not null)
            {
                _singleInstanceCts = new CancellationTokenSource();
                _ = _singleInstanceService.StartListeningAsync(HandleForwardedArgsAsync, _singleInstanceCts.Token);
            }

            _startupCompleted = true;
            _startupDiagnostics.Write("应用启动完成。");
        }
        catch (Exception ex)
        {
            HandleStartupFailure("启动阶段发生异常。", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceCts?.Cancel();
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _singleInstanceService?.Dispose();
        base.OnExit(e);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception ?? new Exception("发生了未处理异常。");
            HandleFatalException("AppDomain 未处理异常。", exception, shutdownAfterReport: !_startupCompleted);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _startupDiagnostics.Write("后台任务发生未观察异常，已记录并标记为已观察。", args.Exception);
            _loggingService.Write("Unobserved background task exception.", args.Exception);
            args.SetObserved();
        };

        DispatcherUnhandledException += (_, args) =>
        {
            HandleFatalException("UI 线程发生未处理异常。", args.Exception, shutdownAfterReport: !_startupCompleted);
            args.Handled = true;
        };
    }

    private void ApplyLocalizationSafe()
    {
        try
        {
            _settings.General.Language = SettingsService.NormalizeLanguage(_settings.General.Language);
            _localizer.ApplyLanguage(_settings.General.Language);
            _startupDiagnostics.Write("本地化服务初始化成功。");
        }
        catch (Exception ex)
        {
            _settings.General.Language = "zh-CN";
            _startupDiagnostics.Write("本地化初始化失败，已回退到简体中文。", ex);
            try
            {
                _localizer.ApplyLanguage("zh-CN");
            }
            catch
            {
            }
        }
    }

    private void ApplyThemeSafe()
    {
        var applied = ThemeService.Instance.TryApplyTheme(_settings.Appearance.Theme, message => _startupDiagnostics.Write(message));
        if (!applied)
        {
            _settings.Appearance.Theme = ThemePreference.Light;
        }

        _startupDiagnostics.Write($"主题应用结果: {ThemeService.Instance.EffectiveTheme}");
    }

    private bool TryInitializeTray()
    {
        if (_trayService is null || _mainViewModel is null || _presetManager is null)
        {
            return false;
        }

        try
        {
            _trayService.Initialize(new TrayContext
            {
                GetCurrentStatus = () => _mainViewModel.StatusText,
                GetCurrentTimeText = () => _mainViewModel.DisplayTime,
                IsMainWindowVisible = () => _mainWindow?.IsVisible == true || _pureWindow?.IsVisible == true,
                ToggleMainWindowVisibility = ToggleMainWindowVisibility,
                TogglePureMode = () => _mainViewModel.TogglePureModeCommand.Execute(null),
                ShowPureWindow = ShowPureWindow,
                ReturnToStandardMode = ReturnToStandardMode,
                StartSit = () => _mainViewModel.StartSitCommand.Execute(null),
                StartStand = () => _mainViewModel.StartStandCommand.Execute(null),
                StartPresetById = id => _mainViewModel.StartPresetById(id),
                GetPresets = () => _presetManager.Presets,
                IsPaused = () => _mainViewModel.IsPaused,
                PauseOrResume = () => _mainViewModel.PauseResumeCommand.Execute(null),
                Stop = () => _mainViewModel.StopCommand.Execute(null),
                Reset = () => _mainViewModel.ResetCommand.Execute(null),
                IsAlwaysOnTop = () => _mainViewModel.IsAlwaysOnTop,
                ToggleAlwaysOnTop = () => _mainViewModel.ToggleAlwaysOnTopCommand.Execute(null),
                IsPureClickThrough = () => _mainViewModel.PureClickThrough,
                SetPureClickThrough = enabled => SetPureClickThroughFromTray(enabled),
                OpenSettings = OpenSettingsWindow,
                Exit = ExitApplication
            });

            EnsureClickThroughSafety();
            _startupDiagnostics.Write($"托盘初始化结果: {_trayService.IsAvailable}");
            return _trayService.IsAvailable;
        }
        catch (Exception ex)
        {
            _startupDiagnostics.Write("托盘初始化失败，应用将强制显示主窗口。", ex);
            EnsureClickThroughSafety();
            return false;
        }
    }

    private void TryAttachTaskbar()
    {
        if (_taskbarService is null || _mainWindow is null || _pureWindow is null)
        {
            return;
        }

        try
        {
            _taskbarService.Attach(_mainWindow, _pureWindow);
            _startupDiagnostics.Write("任务栏服务初始化成功。");
        }
        catch (Exception ex)
        {
            _startupDiagnostics.Write("任务栏服务初始化失败，已忽略。", ex);
        }
    }

    private void ShowStartupWindows(CommandLineOptions options, bool trayAvailable)
    {
        if (_mainWindow is null || _pureWindow is null || _mainViewModel is null)
        {
            return;
        }

        var mode = SafeStartupService.ResolveInitialWindowMode(_settings, options, trayAvailable);
        _startupDiagnostics.Write($"启动显示模式: {mode}");

        switch (mode)
        {
            case StartupWindowMode.HideToTray:
                _mainWindow.Hide();
                _pureWindow.Hide();
                break;
            case StartupWindowMode.ShowPureWindow:
                ApplyPureWindowSettings();
                _pureWindow.Show();
                _pureWindow.Activate();
                _mainWindow.Hide();
                break;
            default:
                _mainWindow.Show();
                _mainWindow.Activate();
                _pureWindow.Hide();
                break;
        }
    }

    private void EnsureVisibleStartupSurface(bool trayAvailable)
    {
        if (_mainWindow is null || _pureWindow is null || _mainViewModel is null)
        {
            return;
        }

        if (!SafeStartupService.ShouldForceShowMainWindow(trayAvailable, _mainWindow.IsVisible, _pureWindow.IsVisible))
        {
            return;
        }

        if (_mainViewModel.IsPureMode)
        {
            _mainViewModel.TogglePureModeCommand.Execute(null);
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        _pureWindow.Hide();
        _startupDiagnostics.Write("托盘不可用或窗口均不可见，已强制显示主窗口。");
    }

    private void HandleStartupFailure(string summary, Exception exception)
    {
        _startupDiagnostics.Write(summary, exception);
        _loggingService.Write(summary, exception);
        ShowFatalMessage(summary, exception);
        Shutdown(-1);
    }

    private void HandleFatalException(string summary, Exception exception, bool shutdownAfterReport)
    {
        _startupDiagnostics.Write(summary, exception);
        _loggingService.Write(summary, exception);

        if (_fatalDialogOpen)
        {
            if (shutdownAfterReport)
            {
                Shutdown(-1);
            }

            return;
        }

        ShowFatalMessage(summary, exception);

        if (shutdownAfterReport)
        {
            Shutdown(-1);
        }
    }

    private void ShowFatalMessage(string summary, Exception exception)
    {
        if (_fatalDialogOpen)
        {
            return;
        }

        _fatalDialogOpen = true;
        try
        {
            var message = $"软件启动失败。{Environment.NewLine}{summary}{Environment.NewLine}{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{Environment.NewLine}日志路径：{_startupDiagnostics.LogPath}";
            System.Windows.MessageBox.Show(message, "MinimalSnapTimer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
        }
        finally
        {
            _fatalDialogOpen = false;
        }
    }

    private async Task HandleForwardedArgsAsync(string[] args)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            var service = new CommandLineService();
            var options = service.Parse(args);
            if (!options.IsValid)
            {
                return;
            }

            _mainViewModel?.ApplyCommandLine(options);
            ShowAppropriateWindow();
        });
    }

    private void ShowAppropriateWindow()
    {
        if (_mainViewModel is null || _mainWindow is null || _pureWindow is null)
        {
            return;
        }

        if (_mainViewModel.IsPureMode)
        {
            _pureWindow.Show();
            _pureWindow.Activate();
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
            _pureWindow.Hide();
        }
    }

    private void ToggleMainWindowVisibility()
    {
        if (_mainWindow is null || _pureWindow is null || _mainViewModel is null)
        {
            return;
        }

        if (_mainViewModel.IsPureMode)
        {
            if (_pureWindow.IsVisible)
            {
                _pureWindow.Hide();
            }
            else
            {
                _pureWindow.Show();
                _pureWindow.Activate();
            }

            return;
        }

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    private void ShowPureWindow()
    {
        if (_mainViewModel is null || _mainWindow is null || _pureWindow is null)
        {
            return;
        }

        if (!_mainViewModel.IsPureMode)
        {
            _mainViewModel.TogglePureModeCommand.Execute(null);
        }

        ApplyPureWindowSettings();
        _pureWindow.Show();
        _pureWindow.Activate();
        _mainWindow.Hide();
    }

    private void ReturnToStandardMode()
    {
        if (_mainViewModel is null || _mainWindow is null || _pureWindow is null)
        {
            return;
        }

        if (_mainViewModel.IsPureMode)
        {
            _mainViewModel.TogglePureModeCommand.Execute(null);
            return;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        _pureWindow.Hide();
    }

    private void SwitchMode(bool pureMode)
    {
        if (_mainWindow is null || _pureWindow is null)
        {
            return;
        }

        if (pureMode)
        {
            ApplyPureWindowSettings();
            _pureWindow.Show();
            _pureWindow.Activate();
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
            _pureWindow.Hide();
        }
    }

    private void OpenSettingsWindow()
    {
        if (_settingsService is null || _mainViewModel is null || _presetManager is null || _trayService is null)
        {
            return;
        }

        var cloned = _settingsService.Clone(_settings);
        var vm = new SettingsViewModel(cloned, _localizer, _appSupportService);
        var window = new SettingsWindow
        {
            DataContext = vm,
            Owner = _mainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        vm.SaveRequested += async (_, _) =>
        {
            if (cloned.Appearance.PureClickThrough && _trayService.IsAvailable != true)
            {
                System.Windows.MessageBox.Show(
                    _localizer["clickThrough.unavailable"],
                    _localizer["app.name"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _settings = cloned;
            SettingsService.EnsureDefaults(_settings);
            ApplyLocalizationSafe();
            ApplyThemeSafe();
            _presetManager.ReplaceSettings(_settings);
            _mainViewModel.ApplySettings(_settings);
            _trayService.RebuildMenu();
            ApplyWindowPlacement();
            ApplyPureWindowSettings();
            ApplyAutoStartSetting();
            RegisterGlobalHotkeysIfNeeded();
            await PersistSettingsAsync();
            window.DialogResult = true;
            window.Close();
        };

        vm.CancelRequested += (_, _) =>
        {
            window.DialogResult = false;
            window.Close();
        };

        window.ShowDialog();
    }

    private void HideToTray()
    {
        if (_trayService?.IsAvailable != true)
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
            _pureWindow?.Hide();
            _startupDiagnostics.Write("托盘不可用，已改为显示主窗口而不是隐藏到托盘。");
            return;
        }

        _trayService.EnsureTrayVisible();
        _mainWindow?.Hide();
        _pureWindow?.Hide();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _mainWindow?.Close();
        _pureWindow?.Close();
        Shutdown();
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        if ((_settings.General.CloseBehavior == CloseBehavior.MinimizeToTray || _settings.Tray.MinimizeToTray) && _trayService?.IsAvailable == true)
        {
            e.Cancel = true;
            HideToTray();
        }
    }

    private void OnPureWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        if (_mainViewModel is not null && _mainViewModel.IsPureMode && _trayService?.IsAvailable == true)
        {
            HideToTray();
        }
        else
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
            _pureWindow?.Hide();
        }
    }

    private void ApplyWindowPlacement()
    {
        if (_mainWindow is null || _pureWindow is null)
        {
            return;
        }

        var mainBounds = WindowPlacementService.ClampToCurrentScreenWorkArea(
            new Rect(_settings.Windows.MainLeft, _settings.Windows.MainTop, _settings.Windows.MainWidth, _settings.Windows.MainHeight));
        var pureBounds = WindowPlacementService.ClampToCurrentScreenWorkArea(
            new Rect(_settings.Windows.PureLeft, _settings.Windows.PureTop, _settings.Windows.PureWidth, _settings.Windows.PureHeight));

        _mainWindow.Left = mainBounds.Left;
        _mainWindow.Top = mainBounds.Top;
        _mainWindow.Width = mainBounds.Width;
        _mainWindow.Height = mainBounds.Height;

        _pureWindow.Left = pureBounds.Left;
        _pureWindow.Top = pureBounds.Top;
        _pureWindow.Width = pureBounds.Width;
        _pureWindow.Height = pureBounds.Height;

        _settings.Windows.MainLeft = mainBounds.Left;
        _settings.Windows.MainTop = mainBounds.Top;
        _settings.Windows.MainWidth = mainBounds.Width;
        _settings.Windows.MainHeight = mainBounds.Height;
        _settings.Windows.PureLeft = pureBounds.Left;
        _settings.Windows.PureTop = pureBounds.Top;
        _settings.Windows.PureWidth = pureBounds.Width;
        _settings.Windows.PureHeight = pureBounds.Height;
    }

    private void ApplyPureWindowSettings()
    {
        if (_pureWindow is null || _mainViewModel is null)
        {
            return;
        }

        _pureWindow.Topmost = _mainViewModel.IsAlwaysOnTop;
        _pureWindow.ShowInTaskbar = _mainViewModel.ShowInTaskbar;
    }

    private void EnsureClickThroughSafety()
    {
        if (_mainViewModel is null || !_settings.Appearance.PureClickThrough)
        {
            return;
        }

        if (_trayService?.IsAvailable == true)
        {
            return;
        }

        _mainViewModel.SetPureClickThrough(false);
        _startupDiagnostics.Write("托盘不可用，已自动关闭点击穿透。");
    }

    private void SetPureClickThroughFromTray(bool enabled)
    {
        if (_mainViewModel is null || _trayService is null)
        {
            return;
        }

        if (enabled && !_trayService.IsAvailable)
        {
            System.Windows.MessageBox.Show(
                _localizer["clickThrough.unavailable"],
                _localizer["app.name"],
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (enabled && !_mainViewModel.PureClickThrough)
        {
            var result = System.Windows.MessageBox.Show(
                _localizer["clickThrough.confirm"],
                _localizer["app.name"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _mainViewModel.SetPureClickThrough(enabled);
        if (enabled)
        {
            ShowPureWindow();
        }
    }

    private void ApplyAutoStartSetting()
    {
        if (_autoStartService is null)
        {
            return;
        }

        try
        {
            _autoStartService.SetEnabled(_settings.General.AutoStartWithWindows, Environment.ProcessPath ?? string.Empty);
            _startupDiagnostics.Write("开机自启动设置已应用。");
        }
        catch (Exception ex)
        {
            _startupDiagnostics.Write("更新开机自启动设置失败。", ex);
            _loggingService.Write("Failed to update auto-start setting.", ex);
        }
    }

    private void RegisterGlobalHotkeysIfNeeded()
    {
        if (_hotkeyService is null || _mainWindow is null)
        {
            return;
        }

        _hotkeyService.Unregister(_mainWindow);
        if (!_settings.Hotkeys.EnableGlobalHotkeys)
        {
            return;
        }

        _hotkeyService.Register(_mainWindow, _settings.Hotkeys.Bindings, action => _mainViewModel?.HandleHotkeyAction(action), out _);
    }

    private async Task PersistSettingsAsync()
    {
        if (_settingsService is null)
        {
            return;
        }

        try
        {
            await _settingsService.SaveAsync(_settings);
            _startupDiagnostics.Write("设置保存成功。");
        }
        catch (Exception ex)
        {
            _startupDiagnostics.Write("设置保存失败。", ex);
            _loggingService.Write("Failed to persist settings.", ex);
        }
    }

    private Task<ReminderResult> ShowReminderDialogAsync(CompletionNotificationRequest request)
    {
        var dispatcher = _pureWindow?.Dispatcher ?? _mainWindow?.Dispatcher ?? Dispatcher;
        return dispatcher.InvokeAsync(() =>
        {
            try
            {
                var window = new ReminderWindow(request);
                if (!TryApplyReminderOwner(window))
                {
                    PlaceReminderWindow(window);
                }

                window.ShowDialog();
                return window.Result;
            }
            catch (Exception ex)
            {
                _startupDiagnostics.Write("提醒弹窗显示失败，已降级为仅保留通知和日志。", ex);
                _loggingService.Write("Reminder dialog failed to show.", ex);
                return new ReminderResult();
            }
        }).Task;
    }

    private bool TryApplyReminderOwner(Window reminderWindow)
    {
        var owner = GetReminderOwnerCandidate();
        if (owner is null)
        {
            return false;
        }

        try
        {
            reminderWindow.Owner = owner;
            reminderWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _startupDiagnostics.Write("提醒弹窗 Owner 不可用，已降级为无 Owner 显示。", ex);
            reminderWindow.Owner = null;
            return false;
        }
    }

    private Window? GetReminderOwnerCandidate()
    {
        if (IsValidReminderOwner(_pureWindow))
        {
            return _pureWindow;
        }

        if (IsValidReminderOwner(_mainWindow))
        {
            return _mainWindow;
        }

        return null;
    }

    private static bool IsValidReminderOwner(Window? owner)
    {
        if (owner is null || !owner.IsLoaded || owner.Visibility != Visibility.Visible)
        {
            return false;
        }

        if (owner.Dispatcher.HasShutdownStarted || owner.Dispatcher.HasShutdownFinished)
        {
            return false;
        }

        try
        {
            return new WindowInteropHelper(owner).Handle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private void PlaceReminderWindow(Window reminderWindow)
    {
        reminderWindow.Owner = null;
        reminderWindow.WindowStartupLocation = WindowStartupLocation.Manual;

        var referenceBounds = GetReminderReferenceBounds(reminderWindow);
        var bounds = WindowPlacementService.CenterOnCurrentScreenWorkArea(
            new System.Windows.Size(reminderWindow.Width, reminderWindow.Height),
            referenceBounds);

        reminderWindow.Left = bounds.Left;
        reminderWindow.Top = bounds.Top;
        _startupDiagnostics.Write("提醒弹窗已使用无 Owner 安全定位策略。");
    }

    private Rect GetReminderReferenceBounds(Window reminderWindow)
    {
        if (_pureWindow is not null && _pureWindow.IsLoaded)
        {
            return new Rect(_pureWindow.Left, _pureWindow.Top, _pureWindow.Width, _pureWindow.Height);
        }

        if (_mainWindow is not null && _mainWindow.IsLoaded)
        {
            return new Rect(_mainWindow.Left, _mainWindow.Top, _mainWindow.Width, _mainWindow.Height);
        }

        return new Rect(0, 0, reminderWindow.Width, reminderWindow.Height);
    }
}




