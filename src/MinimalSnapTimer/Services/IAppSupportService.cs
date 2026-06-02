using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public interface IAppSupportService
{
    string AppVersion { get; }

    string LogDirectory { get; }

    string ConfigDirectory { get; }

    string ExecutablePath { get; }

    void OpenLogDirectory();

    void OpenConfigDirectory();

    void ResetWindowPlacements(AppSettings settings);
}
