using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task NotificationsDisabled_DoesNotSendSystemNotification()
    {
        var sender = new FakeSystemNotificationSender();
        var service = CreateService(sender);
        var request = CreateRequest();
        request.Settings.Reminder.UseToastNotifications = false;

        await service.ShowCompletionAsync(request);

        Assert.Equal(0, sender.CallCount);
    }

    [Fact]
    public async Task Completion_UsesSystemNotification_WhenEnabled()
    {
        var sender = new FakeSystemNotificationSender();
        var service = CreateService(sender);
        var request = CreateRequest();

        await service.ShowCompletionAsync(request);

        Assert.Equal(1, sender.CallCount);
        Assert.Equal(request.Title, sender.LastTitle);
        Assert.Equal(request.Message, sender.LastMessage);
    }

    [Fact]
    public async Task NotificationFailure_DoesNotThrowAndFallsBackToTrayBalloon()
    {
        var sender = new FakeSystemNotificationSender { ThrowOnSend = true };
        var trayCalls = new List<(string Title, string Message)>();
        var failureMessages = new List<string>();
        var service = CreateService(sender);
        service.TrayBalloonCallback = (title, message) => trayCalls.Add((title, message));
        service.NotificationFailureCallback = message => failureMessages.Add(message);
        var request = CreateRequest();

        await service.ShowCompletionAsync(request);

        Assert.Single(failureMessages);
        Assert.Single(trayCalls);
        Assert.Equal(request.Title, trayCalls[0].Title);
        Assert.Equal(request.Message, trayCalls[0].Message);
    }

    private static NotificationService CreateService(ISystemNotificationSender sender)
    {
        return new NotificationService(sender, new LocalizationService());
    }

    private static CompletionNotificationRequest CreateRequest()
    {
        var settings = SettingsService.CreateDefaultSettings();
        settings.Reminder.ShowPopup = false;
        settings.Reminder.PlaySound = false;
        settings.Reminder.UseTrayBalloon = true;
        settings.Reminder.UseToastNotifications = true;

        return new CompletionNotificationRequest
        {
            Title = "计时结束",
            Message = "当前计时已经完成。",
            Settings = settings
        };
    }

    private sealed class FakeSystemNotificationSender : ISystemNotificationSender
    {
        public int CallCount { get; private set; }

        public bool ThrowOnSend { get; init; }

        public string? LastTitle { get; private set; }

        public string? LastMessage { get; private set; }

        public bool TrySend(string title, string message)
        {
            CallCount++;
            LastTitle = title;
            LastMessage = message;

            if (ThrowOnSend)
            {
                throw new InvalidOperationException("notification send failed");
            }

            return true;
        }
    }
}
