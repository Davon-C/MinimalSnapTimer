namespace MinimalSnapTimer.Models;

public sealed class CompletionNotificationRequest
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public WorkflowStage SuggestedNextStage { get; set; } = WorkflowStage.None;

    public bool OfferRepeatCurrentStage { get; set; } = true;

    public bool OfferSnooze { get; set; } = true;

    public AppSettings Settings { get; set; } = new();
}
