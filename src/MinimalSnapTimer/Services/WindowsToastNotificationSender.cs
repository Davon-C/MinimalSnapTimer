using System.Diagnostics;
using System.Security;
using System.Text;

namespace MinimalSnapTimer.Services;

public sealed class WindowsToastNotificationSender : ISystemNotificationSender
{
    public bool TrySend(string title, string message)
    {
        try
        {
            var xml = $"""
                <toast>
                  <visual>
                    <binding template="ToastGeneric">
                      <text>{SecurityElement.Escape(title)}</text>
                      <text>{SecurityElement.Escape(message)}</text>
                    </binding>
                  </visual>
                </toast>
                """;

            var script = $$"""
                Add-Type -AssemblyName System.Runtime.WindowsRuntime | Out-Null
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] > $null
                $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml(@'
                {{xml}}
                '@)
                $toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('MinimalSnapTimer')
                $notifier.Show($toast)
                """;

            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var process = Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {encoded}")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(3000))
            {
                process.Kill(true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
