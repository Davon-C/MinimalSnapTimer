namespace MinimalSnapTimer.Services;

public sealed class NullNotificationSender : ISystemNotificationSender
{
    public bool TrySend(string title, string message) => false;
}
