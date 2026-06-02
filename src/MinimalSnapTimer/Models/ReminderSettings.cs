namespace MinimalSnapTimer.Models;

public sealed class ReminderSettings
{
    public bool ShowPopup { get; set; } = true;

    public bool UseToastNotifications { get; set; } = true;

    public bool UseTrayBalloon { get; set; } = true;

    public bool PlaySound { get; set; } = true;

    public bool LoopSound { get; set; }

    public bool EnableTickingSound { get; set; }

    public NotificationSound Sound { get; set; } = NotificationSound.SystemExclamation;

    public string? CustomSoundFile { get; set; }

    public string? ExternalCommand { get; set; }

    public int SnoozeMinutes { get; set; } = 5;
}
