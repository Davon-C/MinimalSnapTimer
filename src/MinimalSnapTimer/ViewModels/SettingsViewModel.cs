using System.ComponentModel;
using MinimalSnapTimer.Infrastructure;
using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _editableSettings;
    private readonly ILocalizationService _localizer;
    private readonly IAppSupportService _appSupportService;

    public SettingsViewModel(AppSettings settings, ILocalizationService? localizer = null, IAppSupportService? appSupportService = null)
    {
        _editableSettings = settings;
        _localizer = localizer ?? LocalizationService.Instance;
        _appSupportService = appSupportService ?? new AppSupportService(new SettingsService(AppContext.BaseDirectory), new StartupDiagnosticsService());
        _localizer.PropertyChanged += OnLocalizerPropertyChanged;
        SaveCommand = new RelayCommand(() => SaveRequested?.Invoke(this, EventArgs.Empty));
        CancelCommand = new RelayCommand(() => CancelRequested?.Invoke(this, EventArgs.Empty));
        OpenLogDirectoryCommand = new RelayCommand(() => _appSupportService.OpenLogDirectory());
        OpenConfigDirectoryCommand = new RelayCommand(() => _appSupportService.OpenConfigDirectory());
        ResetWindowPositionsCommand = new RelayCommand(ResetWindowPositions);
    }

    public event EventHandler? SaveRequested;

    public event EventHandler? CancelRequested;

    public RelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand OpenLogDirectoryCommand { get; }

    public RelayCommand OpenConfigDirectoryCommand { get; }

    public RelayCommand ResetWindowPositionsCommand { get; }

    public AppSettings Settings => _editableSettings;

    public string VersionText => $"MinimalSnapTimer v{_appSupportService.AppVersion}";

    public string LogDirectoryPath => _appSupportService.LogDirectory;

    public string ConfigDirectoryPath => _appSupportService.ConfigDirectory;

    public string ExecutablePath => _appSupportService.ExecutablePath;

    public int SettingsVersion => _editableSettings.SettingsVersion;

    public IReadOnlyList<KeyValuePair<PresetType, string>> PresetTypeOptions =>
    [
        new(PresetType.Normal, _localizer["preset.normal"]),
        new(PresetType.Sit, _localizer["preset.sit"]),
        new(PresetType.Stand, _localizer["preset.stand"]),
        new(PresetType.Break, _localizer["preset.break"]),
        new(PresetType.Focus, _localizer["preset.focus"]),
        new(PresetType.Custom, _localizer["preset.custom"])
    ];

    public IReadOnlyList<KeyValuePair<ThemePreference, string>> ThemeOptions =>
    [
        new(ThemePreference.System, _localizer["settings.theme.system"]),
        new(ThemePreference.Light, _localizer["settings.theme.light"]),
        new(ThemePreference.Dark, _localizer["settings.theme.dark"])
    ];

    public IReadOnlyList<KeyValuePair<string, string>> LanguageOptions =>
    [
        new("zh-CN", _localizer["settings.language.zh-CN"]),
        new("en-US", _localizer["settings.language.en-US"])
    ];

    private void OnLocalizerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(LocalizationService.CurrentLanguage) and not "Item[]")
        {
            return;
        }

        OnPropertyChanged(nameof(PresetTypeOptions));
        OnPropertyChanged(nameof(ThemeOptions));
        OnPropertyChanged(nameof(LanguageOptions));
    }

    private void ResetWindowPositions()
    {
        _appSupportService.ResetWindowPlacements(_editableSettings);
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(SettingsVersion));
    }
}
