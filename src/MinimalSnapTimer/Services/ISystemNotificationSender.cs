namespace MinimalSnapTimer.Services;

public interface ISystemNotificationSender
{
    bool TrySend(string title, string message);
}
