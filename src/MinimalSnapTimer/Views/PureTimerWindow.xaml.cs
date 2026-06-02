using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MinimalSnapTimer.Infrastructure;
using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;

namespace MinimalSnapTimer.Views;

public partial class PureTimerWindow : Window
{
    private bool _isAdjustingPlacement;
    private bool _isSyncingClickThrough;

    public PureTimerWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureSafePlacement();
        PersistPlacement();
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ToolbarBorder.Visibility = Visibility.Visible;
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ToolbarBorder.Visibility = Visibility.Collapsed;
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (ViewModel.Snapshot.State == TimerState.Completed && ViewModel.HasSuggestedNextStage)
        {
            ViewModel.ActivateSuggestedStageCommand.Execute(null);
            return;
        }

        if (!ViewModel.Settings.Appearance.LockPureWindowPosition)
        {
            DragMove();
            EnsureSafePlacement();
            PersistPlacement();
        }
    }

    private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ViewModel?.TogglePureModeCommand.Execute(null);
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

    private void ToggleClickThroughMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (!ViewModel.PureClickThrough)
        {
            if (!ViewModel.CanEnablePureClickThrough())
            {
                System.Windows.MessageBox.Show(
                    LocalizationService.Instance["clickThrough.unavailable"],
                    LocalizationService.Instance["app.name"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                LocalizationService.Instance["clickThrough.confirm"],
                LocalizationService.Instance["app.name"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        ViewModel.SetPureClickThrough(!ViewModel.PureClickThrough);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += ViewModelOnPropertyChanged;
            ApplyClickThroughFromViewModel(newVm.PureClickThrough);
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PureClickThrough) && ViewModel is not null)
        {
            ApplyClickThroughFromViewModel(ViewModel.PureClickThrough);
        }
    }

    private void ApplyClickThroughFromViewModel(bool enabled)
    {
        if (_isSyncingClickThrough)
        {
            return;
        }

        if (TryApplyClickThrough(enabled, out var errorMessage))
        {
            return;
        }

        System.Windows.MessageBox.Show(
            errorMessage,
            LocalizationService.Instance["app.name"],
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        if (!enabled || ViewModel is null)
        {
            return;
        }

        _isSyncingClickThrough = true;
        try
        {
            ViewModel.SetPureClickThrough(false);
        }
        finally
        {
            _isSyncingClickThrough = false;
        }
    }

    private bool TryApplyClickThrough(bool enabled, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var handle = new WindowInteropHelper(this).EnsureHandle();
            Marshal.GetLastWin32Error();
            var style = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
            if (enabled)
            {
                style |= NativeMethods.WsExTransparent;
            }
            else
            {
                style &= ~NativeMethods.WsExTransparent;
            }

            var result = NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, style);
            if (result == 0 && Marshal.GetLastWin32Error() != 0)
            {
                errorMessage = LocalizationService.Instance["clickThrough.toggleFailed"];
                return false;
            }

            return true;
        }
        catch
        {
            errorMessage = LocalizationService.Instance["clickThrough.toggleFailed"];
            return false;
        }
    }

    private void EnsureSafePlacement()
    {
        var corrected = WindowPlacementService.SnapAndClampToCurrentScreenWorkArea(new Rect(Left, Top, Width, Height));
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

        ViewModel.Settings.Windows.PureLeft = Left;
        ViewModel.Settings.Windows.PureTop = Top;
        ViewModel.Settings.Windows.PureWidth = Width;
        ViewModel.Settings.Windows.PureHeight = Height;
        ViewModel.NotifySettingsChanged();
    }
}
