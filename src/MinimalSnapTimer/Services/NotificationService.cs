using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows.Media;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class NotificationService : INotificationService
{
    private readonly Lazy<MediaPlayer> _mediaPlayer = new(() => new MediaPlayer());
    private readonly ILocalizationService _localizer;

    public NotificationService(ISystemNotificationSender? notificationSender = null, ILocalizationService? localizer = null)
    {
        SystemNotificationSender = notificationSender ?? new WindowsToastNotificationSender();
        _localizer = localizer ?? LocalizationService.Instance;
    }

    public ISystemNotificationSender SystemNotificationSender { get; set; }

    public Func<CompletionNotificationRequest, Task<ReminderResult>>? ReminderDialogFactory { get; set; }

    public Action<string, string>? TrayBalloonCallback { get; set; }

    public Action<string>? NotificationFailureCallback { get; set; }

    public async Task<ReminderResult> ShowCompletionAsync(CompletionNotificationRequest request)
    {
        var notificationShown = false;

        if (request.Settings.Reminder.UseToastNotifications)
        {
            notificationShown = TryShowSystemNotification(request.Title, request.Message);
        }

        if (!notificationShown && request.Settings.Reminder.UseTrayBalloon)
        {
            TryShowTrayBalloon(request.Title, request.Message);
        }

        if (request.Settings.Reminder.PlaySound)
        {
            PlaySound(request.Settings.Reminder);
        }

        if (!string.IsNullOrWhiteSpace(request.Settings.Reminder.ExternalCommand))
        {
            TryRunExternalCommand(request.Settings.Reminder.ExternalCommand!);
        }

        if (request.Settings.Reminder.ShowPopup && ReminderDialogFactory is not null)
        {
            return await ReminderDialogFactory(request);
        }

        return new ReminderResult { Action = ReminderAction.None };
    }

    public void StopLoopingSound()
    {
        if (_mediaPlayer.IsValueCreated)
        {
            _mediaPlayer.Value.Stop();
        }
    }

    private bool TryShowSystemNotification(string title, string message)
    {
        try
        {
            return SystemNotificationSender.TrySend(title, message);
        }
        catch
        {
            NotificationFailureCallback?.Invoke(_localizer["error.notificationFailed"]);
            return false;
        }
    }

    private void TryShowTrayBalloon(string title, string message)
    {
        try
        {
            TrayBalloonCallback?.Invoke(title, message);
        }
        catch
        {
        }
    }

    private void PlaySound(ReminderSettings settings)
    {
        if (settings.Sound == NotificationSound.Custom && !string.IsNullOrWhiteSpace(settings.CustomSoundFile) && File.Exists(settings.CustomSoundFile))
        {
            var player = _mediaPlayer.Value;
            player.Open(new Uri(settings.CustomSoundFile, UriKind.Absolute));
            player.Position = TimeSpan.Zero;
            player.MediaEnded -= OnMediaEnded;
            if (settings.LoopSound)
            {
                player.MediaEnded += OnMediaEnded;
            }

            player.Play();
            return;
        }

        switch (settings.Sound)
        {
            case NotificationSound.SystemAsterisk:
                SystemSounds.Asterisk.Play();
                break;
            case NotificationSound.SystemBeep:
                SystemSounds.Beep.Play();
                break;
            case NotificationSound.SystemHand:
                SystemSounds.Hand.Play();
                break;
            case NotificationSound.SystemQuestion:
                SystemSounds.Question.Play();
                break;
            default:
                SystemSounds.Exclamation.Play();
                break;
        }
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (!_mediaPlayer.IsValueCreated)
        {
            return;
        }

        _mediaPlayer.Value.Position = TimeSpan.Zero;
        _mediaPlayer.Value.Play();
    }

    private static void TryRunExternalCommand(string command)
    {
        try
        {
            var parts = command.Split(' ', 2, StringSplitOptions.TrimEntries);
            var fileName = parts[0];
            var arguments = parts.Length > 1 ? parts[1] : string.Empty;
            Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}
