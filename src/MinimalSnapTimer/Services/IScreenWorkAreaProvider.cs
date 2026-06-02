using System.Windows;

namespace MinimalSnapTimer.Services;

public interface IScreenWorkAreaProvider
{
    IReadOnlyList<ScreenWorkArea> GetScreens();

    ScreenWorkArea GetNearestScreen(Rect windowBounds);
}
