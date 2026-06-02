using Microsoft.Win32;

namespace MinimalSnapTimer.Services;

public sealed class AutoStartService
{
    private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MinimalSnapTimer";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunPath);
        return key?.GetValue(AppName) is string;
    }

    public void SetEnabled(bool enabled, string executablePath, string? arguments = null)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunPath);
        if (enabled)
        {
            var value = $"\"{executablePath}\"";
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                value = $"{value} {arguments}";
            }

            key.SetValue(AppName, value);
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
