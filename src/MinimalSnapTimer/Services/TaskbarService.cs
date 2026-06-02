using System.Windows;
using System.Windows.Shell;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class TaskbarService
{
    private Window? _mainWindow;
    private Window? _pureWindow;

    public void Attach(Window mainWindow, Window pureWindow)
    {
        _mainWindow = mainWindow;
        _pureWindow = pureWindow;
        _mainWindow.TaskbarItemInfo ??= new TaskbarItemInfo();
        _pureWindow.TaskbarItemInfo ??= new TaskbarItemInfo();
    }

    public void Update(TimerSnapshot snapshot, string title)
    {
        UpdateWindow(_mainWindow, snapshot, title);
        UpdateWindow(_pureWindow, snapshot, title);
    }

    private static void UpdateWindow(Window? window, TimerSnapshot snapshot, string title)
    {
        if (window is null)
        {
            return;
        }

        window.Dispatcher.Invoke(() =>
        {
            window.Title = title;
            if (window.TaskbarItemInfo is null)
            {
                window.TaskbarItemInfo = new TaskbarItemInfo();
            }

            var item = window.TaskbarItemInfo;
            if (snapshot.Mode == TimerMode.Countdown && snapshot.InitialDuration.TotalSeconds > 0)
            {
                item.ProgressValue = Math.Clamp(snapshot.CurrentTime.TotalSeconds / snapshot.InitialDuration.TotalSeconds, 0d, 1d);
            }
            else
            {
                item.ProgressValue = 0d;
            }

            item.ProgressState = snapshot.State switch
            {
                TimerState.Running when snapshot.Mode == TimerMode.Countdown => TaskbarItemProgressState.Normal,
                TimerState.Paused => TaskbarItemProgressState.Paused,
                TimerState.Completed => TaskbarItemProgressState.Error,
                _ => TaskbarItemProgressState.None
            };
        });
    }
}
