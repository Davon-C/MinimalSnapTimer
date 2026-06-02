using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Views;

public partial class ReminderWindow : Window
{
    public ReminderWindow(CompletionNotificationRequest request)
    {
        InitializeComponent();
        TitleBlock.Text = request.Title;
        MessageBlock.Text = request.Message;

        if (request.SuggestedNextStage == WorkflowStage.None)
        {
            SuggestedButtonVisibility(false);
        }
    }

    public ReminderResult Result { get; private set; } = new();

    private void Suggested_Click(object sender, RoutedEventArgs e)
    {
        Result = new ReminderResult { Action = ReminderAction.StartSuggestedStage };
        Close();
    }

    private void Repeat_Click(object sender, RoutedEventArgs e)
    {
        Result = new ReminderResult { Action = ReminderAction.RepeatCurrentStage };
        Close();
    }

    private void Snooze_Click(object sender, RoutedEventArgs e)
    {
        Result = new ReminderResult { Action = ReminderAction.Snooze };
        Close();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        Result = new ReminderResult { Action = ReminderAction.Stop };
        Close();
    }

    private void SuggestedButtonVisibility(bool visible)
    {
        if (Content is not Grid grid || grid.Children.Count == 0)
        {
            return;
        }

        if (grid.Children.OfType<UniformGrid>().FirstOrDefault() is not UniformGrid buttons)
        {
            return;
        }

        buttons.Children[0].Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}
