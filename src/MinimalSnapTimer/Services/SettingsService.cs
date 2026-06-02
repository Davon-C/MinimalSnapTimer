using System.IO;
using System.Text.Json;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class SettingsService
{
    public const int CurrentSettingsVersion = 2;

    private readonly string _appBaseDirectory;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SettingsService(string? appBaseDirectory = null)
    {
        _appBaseDirectory = appBaseDirectory ?? AppContext.BaseDirectory;
    }

    public string SettingsPath => ResolveSettingsPath();

    public AppSettings Current { get; private set; } = CreateDefaultSettings();

    public async Task<AppSettings> LoadAsync()
    {
        var path = ResolveSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!File.Exists(path))
        {
            Current = CreateDefaultSettings();
            await SaveAsync(Current);
            return Current;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, _serializerOptions);
            Current = settings ?? CreateDefaultSettings();
            EnsureDefaults(Current);
            return Current;
        }
        catch
        {
            var backup = $"{path}.broken-{DateTime.Now:yyyyMMddHHmmss}";
            File.Copy(path, backup, true);
            Current = CreateDefaultSettings();
            await SaveAsync(Current);
            return Current;
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        EnsureDefaults(settings);
        var path = ResolveSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, settings, _serializerOptions);
        Current = settings;
    }

    public AppSettings Clone(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        return JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions) ?? CreateDefaultSettings();
    }

    public static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            SettingsVersion = CurrentSettingsVersion,
            Presets = new List<TimerPreset>
            {
                new() { Name = "\u5750\u7740\u5de5\u4f5c", DurationMinutes = 40, Type = PresetType.Sit, AutoStart = true },
                new() { Name = "\u7ad9\u7acb\u6d3b\u52a8", DurationMinutes = 15, Type = PresetType.Stand, AutoStart = true },
                new() { Name = "\u4e13\u6ce8\u5de5\u4f5c", DurationMinutes = 25, Type = PresetType.Focus, AutoStart = true },
                new() { Name = "\u77ed\u4f11\u606f", DurationMinutes = 5, Type = PresetType.Break, AutoStart = true },
                new() { Name = "\u957f\u4f11\u606f", DurationMinutes = 15, Type = PresetType.Break, AutoStart = true }
            },
            Hotkeys = new HotkeySettings
            {
                Bindings = new List<HotkeyBinding>
                {
                    new() { Action = "StartPauseResume", Gesture = "Space" },
                    new() { Action = "Reset", Gesture = "R" },
                    new() { Action = "Stop", Gesture = "S" },
                    new() { Action = "ToggleTopmost", Gesture = "T" },
                    new() { Action = "MinimizeToTray", Gesture = "M" },
                    new() { Action = "TogglePureMode", Gesture = "P" },
                    new() { Action = "StartSit", Gesture = "Ctrl+1" },
                    new() { Action = "StartStand", Gesture = "Ctrl+2" },
                    new() { Action = "HideWindow", Gesture = "Esc" }
                }
            }
        };
    }

    public static void EnsureDefaults(AppSettings settings)
    {
        if (settings.SettingsVersion <= 0)
        {
            settings.SettingsVersion = 1;
        }

        settings.General ??= new GeneralSettings();
        settings.Timing ??= new TimingSettings();
        settings.Appearance ??= new AppearanceSettings();
        settings.Reminder ??= new ReminderSettings();
        settings.Tray ??= new TraySettings();
        settings.Hotkeys ??= new HotkeySettings();
        settings.Windows ??= new WindowPlacementSettings();
        settings.Presets ??= new List<TimerPreset>();

        if (settings.Presets.Count == 0)
        {
            settings.Presets = CreateDefaultSettings().Presets;
        }

        if (settings.Hotkeys.Bindings.Count == 0)
        {
            settings.Hotkeys.Bindings = CreateDefaultSettings().Hotkeys.Bindings;
        }

        settings.General.Language = NormalizeLanguage(settings.General.Language);

        if (!Enum.IsDefined(typeof(ThemePreference), settings.Appearance.Theme))
        {
            settings.Appearance.Theme = ThemePreference.Light;
        }

        if (!Enum.IsDefined(typeof(CloseBehavior), settings.General.CloseBehavior))
        {
            settings.General.CloseBehavior = CloseBehavior.MinimizeToTray;
        }

        if (!Enum.IsDefined(typeof(NotificationSound), settings.Reminder.Sound))
        {
            settings.Reminder.Sound = NotificationSound.SystemExclamation;
        }

        MigrateSettings(settings);
        SynchronizeWorkflowDurations(settings);
        settings.SettingsVersion = CurrentSettingsVersion;
    }

    public static void MigrateSettings(AppSettings settings)
    {
        if (settings.SettingsVersion < 2)
        {
            settings.General.Language = NormalizeLanguage(settings.General.Language);
            settings.Appearance.WindowOpacity = Math.Clamp(settings.Appearance.WindowOpacity, 0.3d, 1d);
            settings.Appearance.PureBackgroundOpacity = Math.Clamp(settings.Appearance.PureBackgroundOpacity, 0.3d, 1d);
            settings.Reminder.SnoozeMinutes = Math.Max(1, settings.Reminder.SnoozeMinutes);
            settings.SettingsVersion = 2;
        }
    }

    public static string NormalizeLanguage(string? language)
    {
        return string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";
    }

    public static void SynchronizeWorkflowDurations(AppSettings settings)
    {
        var sitPreset = FindWorkflowPreset(settings.Presets, WorkflowStage.Sit);
        var standPreset = FindWorkflowPreset(settings.Presets, WorkflowStage.Stand);

        if (sitPreset is not null)
        {
            settings.Timing.SitMinutes = sitPreset.DurationMinutes;
        }

        if (standPreset is not null)
        {
            settings.Timing.StandMinutes = standPreset.DurationMinutes;
        }
    }

    private static TimerPreset? FindWorkflowPreset(IEnumerable<TimerPreset> presets, WorkflowStage stage)
    {
        var (name, type) = stage switch
        {
            WorkflowStage.Sit => ("\u5750\u7740\u5de5\u4f5c", PresetType.Sit),
            WorkflowStage.Stand => ("\u7ad9\u7acb\u6d3b\u52a8", PresetType.Stand),
            _ => (string.Empty, PresetType.Normal)
        };

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? presets.FirstOrDefault(p => p.Type == type);
    }

    private string ResolveSettingsPath()
    {
        var portableFlag = Path.Combine(_appBaseDirectory, "portable.flag");
        if (File.Exists(portableFlag))
        {
            return Path.Combine(_appBaseDirectory, "settings.json");
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "MinimalSnapTimer", "settings.json");
    }
}
