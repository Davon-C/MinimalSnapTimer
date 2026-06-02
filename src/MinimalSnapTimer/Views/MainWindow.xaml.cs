using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;

namespace MinimalSnapTimer.Views;

public partial class MainWindow : Window
{
    private bool _isAdjustingPlacement;

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureSafePlacement();
        PersistPlacement();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (ViewModel?.Settings.Hotkeys.EnableWindowHotkeys != true)
        {
            return;
        }

        if (e.Key == Key.Space)
        {
            ViewModel.HandleHotkeyAction("StartPauseResume");
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D1)
        {
            ViewModel.HandleHotkeyAction("StartSit");
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D2)
        {
            ViewModel.HandleHotkeyAction("StartStand");
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.R:
                ViewModel.HandleHotkeyAction("Reset");
                e.Handled = true;
                break;
            case Key.S:
                ViewModel.HandleHotkeyAction("Stop");
                e.Handled = true;
                break;
            case Key.T:
                ViewModel.HandleHotkeyAction("ToggleTopmost");
                e.Handled = true;
                break;
            case Key.M:
            case Key.Escape:
                ViewModel.HandleHotkeyAction("HideWindow");
                e.Handled = true;
                break;
            case Key.P:
                ViewModel.HandleHotkeyAction("TogglePureMode");
                e.Handled = true;
                break;
        }
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (ViewModel is null || _isAdjustingPlacement)
        {
            return;
        }

        EnsureSafePlacement();
        PersistPlacement();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ViewModel is null || WindowState != WindowState.Normal || _isAdjustingPlacement)
        {
            return;
        }

        EnsureSafePlacement();
        PersistPlacement();
    }

    private void PresetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is null || sender is not System.Windows.Controls.ListBox listBox || listBox.SelectedItem is not TimerPreset preset)
        {
            return;
        }

        ViewModel.StartPresetById(preset.Id);
    }

    private void EnsureSafePlacement()
    {
        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(new Rect(Left, Top, Width, Height));

        ApplyPlacement(corrected);
    }

    private void ApplyPlacement(Rect placement)
    {
        if (Math.Abs(Left - placement.Left) < 0.1
            && Math.Abs(Top - placement.Top) < 0.1
            && Math.Abs(Width - placement.Width) < 0.1
            && Math.Abs(Height - placement.Height) < 0.1)
        {
            return;
        }

        _isAdjustingPlacement = true;
        try
        {
            Left = placement.Left;
            Top = placement.Top;
            Width = placement.Width;
            Height = placement.Height;
        }
        finally
        {
            _isAdjustingPlacement = false;
        }
    }

    private void PersistPlacement()
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.Settings.Windows.MainLeft = Left;
        ViewModel.Settings.Windows.MainTop = Top;
        ViewModel.Settings.Windows.MainWidth = Width;
        ViewModel.Settings.Windows.MainHeight = Height;
        ViewModel.NotifySettingsChanged();
    }
}
