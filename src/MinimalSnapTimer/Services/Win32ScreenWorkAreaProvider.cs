using System.Runtime.InteropServices;
using System.Windows;
using MinimalSnapTimer.Infrastructure;

namespace MinimalSnapTimer.Services;

public sealed class Win32ScreenWorkAreaProvider : IScreenWorkAreaProvider
{
    public IReadOnlyList<ScreenWorkArea> GetScreens()
    {
        var screens = new List<ScreenWorkArea>();

        NativeMethods.EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref NativeMethods.RECT __, IntPtr ___) => CollectScreen(hMonitor, screens),
            IntPtr.Zero);

        if (screens.Count == 0)
        {
            screens.Add(new ScreenWorkArea(SystemParameters.WorkArea, SystemParameters.WorkArea));
        }

        return screens;
    }

    private static bool CollectScreen(IntPtr hMonitor, List<ScreenWorkArea> screens)
    {
        if (TryGetScreen(hMonitor, out var screen))
        {
            screens.Add(screen);
        }

        return true;
    }

    public ScreenWorkArea GetNearestScreen(Rect windowBounds)
    {
        var rect = ToNativeRect(windowBounds);
        var monitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero && TryGetScreen(monitor, out var screen))
        {
            return screen;
        }

        return GetScreens().First();
    }

    private static bool TryGetScreen(IntPtr hMonitor, out ScreenWorkArea screen)
    {
        var info = new NativeMethods.MONITORINFO
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>()
        };

        if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
        {
            screen = new ScreenWorkArea(ToRect(info.rcMonitor), ToRect(info.rcWork));
            return true;
        }

        screen = default;
        return false;
    }

    private static NativeMethods.RECT ToNativeRect(Rect rect)
    {
        return new NativeMethods.RECT
        {
            Left = (int)Math.Floor(rect.Left),
            Top = (int)Math.Floor(rect.Top),
            Right = (int)Math.Ceiling(rect.Right),
            Bottom = (int)Math.Ceiling(rect.Bottom)
        };
    }

    private static Rect ToRect(NativeMethods.RECT rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
}
