using System.Windows;

namespace MinimalSnapTimer.Services;

public static class WindowPlacementService
{
    private static readonly IScreenWorkAreaProvider DefaultProvider = new Win32ScreenWorkAreaProvider();

    public static Rect ClampToWorkArea(Rect windowBounds, Rect workArea)
    {
        var safeWidth = Math.Min(Math.Max(windowBounds.Width, 1d), workArea.Width);
        var safeHeight = Math.Min(Math.Max(windowBounds.Height, 1d), workArea.Height);

        var maxLeft = workArea.Right - safeWidth;
        var maxTop = workArea.Bottom - safeHeight;

        var left = Math.Clamp(windowBounds.Left, workArea.Left, maxLeft);
        var top = Math.Clamp(windowBounds.Top, workArea.Top, maxTop);

        return new Rect(left, top, safeWidth, safeHeight);
    }

    public static Rect SnapAndClampToWorkArea(Rect windowBounds, Rect workArea, double threshold = 16d)
    {
        var corrected = ClampToWorkArea(windowBounds, workArea);
        var left = corrected.Left;
        var top = corrected.Top;

        if (Math.Abs(left - workArea.Left) <= threshold)
        {
            left = workArea.Left;
        }
        else if (Math.Abs(corrected.Right - workArea.Right) <= threshold)
        {
            left = workArea.Right - corrected.Width;
        }

        if (Math.Abs(top - workArea.Top) <= threshold)
        {
            top = workArea.Top;
        }
        else if (Math.Abs(corrected.Bottom - workArea.Bottom) <= threshold)
        {
            top = workArea.Bottom - corrected.Height;
        }

        return ClampToWorkArea(new Rect(left, top, corrected.Width, corrected.Height), workArea);
    }

    public static Rect ClampToCurrentScreenWorkArea(Rect windowBounds, IScreenWorkAreaProvider? provider = null)
    {
        var resolvedProvider = provider ?? DefaultProvider;
        var workArea = resolvedProvider.GetNearestScreen(windowBounds).WorkArea;
        return ClampToWorkArea(windowBounds, workArea);
    }

    public static Rect SnapAndClampToCurrentScreenWorkArea(Rect windowBounds, IScreenWorkAreaProvider? provider = null, double threshold = 16d)
    {
        var resolvedProvider = provider ?? DefaultProvider;
        var screens = resolvedProvider.GetScreens();

        if (CountIntersectingScreens(windowBounds, screens) > 1)
        {
            return windowBounds;
        }

        var workArea = resolvedProvider.GetNearestScreen(windowBounds).WorkArea;
        return SnapAndClampToWorkArea(windowBounds, workArea, threshold);
    }

    public static Rect CenterOnCurrentScreenWorkArea(System.Windows.Size windowSize, Rect referenceBounds, IScreenWorkAreaProvider? provider = null)
    {
        var resolvedProvider = provider ?? DefaultProvider;
        var workArea = resolvedProvider.GetNearestScreen(referenceBounds).WorkArea;
        return CenterInWorkArea(windowSize, workArea);
    }

    public static Rect CenterInWorkArea(System.Windows.Size windowSize, Rect workArea)
    {
        var safeWidth = Math.Min(Math.Max(windowSize.Width, 1d), workArea.Width);
        var safeHeight = Math.Min(Math.Max(windowSize.Height, 1d), workArea.Height);
        var left = workArea.Left + ((workArea.Width - safeWidth) / 2d);
        var top = workArea.Top + ((workArea.Height - safeHeight) / 2d);
        return new Rect(left, top, safeWidth, safeHeight);
    }

    private static int CountIntersectingScreens(Rect windowBounds, IReadOnlyList<ScreenWorkArea> screens)
    {
        var count = 0;
        foreach (var screen in screens)
        {
            if (screen.Bounds.IntersectsWith(windowBounds))
            {
                count++;
            }
        }

        return count;
    }
}
