namespace MinimalSnapTimer.Services;

public interface INotificationService
{
    Task<MinimalSnapTimer.Models.ReminderResult> ShowCompletionAsync(MinimalSnapTimer.Models.CompletionNotificationRequest request);

    void StopLoopingSound();
}
