using System.Diagnostics;
using System.IO;
using System.Reflection;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class AppSupportService : IAppSupportService
{
    private readonly SettingsService _settingsService;
    private readonly StartupDiagnosticsService _startupDiagnostics;
    private readonly Action<string> _directoryOpener;
    private readonly Func<Assembly> _assemblyProvider;
    private readonly string _executablePath;

    public AppSupportService(
        SettingsService settingsService,
        StartupDiagnosticsService startupDiagnostics,
        Action<string>? directoryOpener = null,
        Func<Assembly>? assemblyProvider = null,
        string? executablePath = null)
    {
        _settingsService = settingsService;
        _startupDiagnostics = startupDiagnostics;
        _directoryOpener = directoryOpener ?? OpenDirectoryWithExplorer;
        _assemblyProvider = assemblyProvider ?? (() => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());
        var assembly = _assemblyProvider();
        var assemblyName = assembly.GetName().Name ?? "MinimalSnapTimer";
        _executablePath = executablePath
            ?? Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.exe");
    }

    public string AppVersion => ResolveVersion();

    public string LogDirectory => _startupDiagnostics.LogDirectory;

    public string ConfigDirectory => Path.GetDirectoryName(_settingsService.SettingsPath) ?? AppContext.BaseDirectory;

    public string ExecutablePath => _executablePath;

    public void OpenLogDirectory()
    {
        Directory.CreateDirectory(LogDirectory);
        _directoryOpener(LogDirectory);
    }

    public void OpenConfigDirectory()
    {
        Directory.CreateDirectory(ConfigDirectory);
        _directoryOpener(ConfigDirectory);
    }

    public void ResetWindowPlacements(AppSettings settings)
    {
        settings.Windows = new WindowPlacementSettings();
        SettingsService.EnsureDefaults(settings);
    }

    private string ResolveVersion()
    {
        var assembly = _assemblyProvider();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            return infoVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "0.1.0";
    }

    private static void OpenDirectoryWithExplorer(string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }
}
