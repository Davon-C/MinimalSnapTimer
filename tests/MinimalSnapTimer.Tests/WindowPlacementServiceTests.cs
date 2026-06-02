using System.Windows;
using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class WindowPlacementServiceTests
{
    [Fact]
    public void SingleScreen_StillClampsInsideWorkArea()
    {
        var provider = new FakeScreenWorkAreaProvider(
            new ScreenWorkArea(new Rect(0, 0, 1920, 1080), new Rect(0, 0, 1920, 1040)));
        var windowBounds = new Rect(1900, 1030, 320, 120);

        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(windowBounds, provider);

        Assert.Equal(1600, corrected.Left, 3);
        Assert.Equal(920, corrected.Top, 3);
        Assert.True(corrected.Right <= 1920);
        Assert.True(corrected.Bottom <= 1040);
    }

    [Fact]
    public void DualScreen_WindowInsideSecondaryWorkArea_DoesNotReturnToPrimary()
    {
        var provider = CreateDualScreenProvider();
        var windowBounds = new Rect(2100, 100, 320, 120);

        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(windowBounds, provider);

        Assert.Equal(2100, corrected.Left, 3);
        Assert.Equal(100, corrected.Top, 3);
        Assert.True(corrected.Left >= 1920);
    }

    [Fact]
    public void DualScreen_WindowNearSecondaryTaskbar_UsesSecondaryWorkArea()
    {
        var provider = CreateDualScreenProvider();
        var windowBounds = new Rect(2200, 1010, 320, 120);

        var corrected = WindowPlacementService.SnapAndClampToCurrentScreenWorkArea(windowBounds, provider);

        Assert.Equal(920, corrected.Top, 3);
        Assert.Equal(1040, corrected.Bottom, 3);
    }

    [Fact]
    public void SavedPositionOnSecondary_RestoresOnSecondary()
    {
        var provider = CreateDualScreenProvider();
        var savedBounds = new Rect(2400, 220, 320, 120);

        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(savedBounds, provider);

        Assert.Equal(2400, corrected.Left, 3);
        Assert.Equal(220, corrected.Top, 3);
        Assert.True(corrected.Left >= 1920);
    }

    [Fact]
    public void SavedPositionOnDisconnectedSecondary_FallsBackToAvailableScreen()
    {
        var provider = new FakeScreenWorkAreaProvider(
            new ScreenWorkArea(new Rect(0, 0, 1920, 1080), new Rect(0, 0, 1920, 1040)));
        var savedBounds = new Rect(2400, 220, 320, 120);

        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(savedBounds, provider);

        Assert.Equal(1600, corrected.Left, 3);
        Assert.Equal(220, corrected.Top, 3);
        Assert.True(corrected.Right <= 1920);
    }

    [Fact]
    public void LiveCrossScreenMove_DoesNotSnapBackToPrimary()
    {
        var provider = CreateDualScreenProvider();
        var inTransitBounds = new Rect(1700, 100, 320, 120);

        var corrected = WindowPlacementService.SnapAndClampToCurrentScreenWorkArea(inTransitBounds, provider);

        Assert.Equal(inTransitBounds.Left, corrected.Left, 3);
        Assert.Equal(inTransitBounds.Top, corrected.Top, 3);
    }

    [Fact]
    public void StandardWindowRestore_IsNotIncorrectlyLimitedToPrimary()
    {
        var provider = CreateDualScreenProvider();
        var savedBounds = new Rect(2500, 300, 480, 300);

        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(savedBounds, provider);

        Assert.Equal(2500, corrected.Left, 3);
        Assert.Equal(300, corrected.Top, 3);
    }

    [Fact]
    public void OversizedWindow_IsReducedToCurrentScreenWorkArea()
    {
        var provider = CreateDualScreenProvider();
        var oversized = new Rect(2100, 100, 2400, 1600);

        var corrected = WindowPlacementService.ClampToCurrentScreenWorkArea(oversized, provider);

        Assert.Equal(1920, corrected.Left, 3);
        Assert.Equal(0, corrected.Top, 3);
        Assert.Equal(1920, corrected.Width, 3);
        Assert.Equal(1040, corrected.Height, 3);
    }

    [Fact]
    public void ReminderFallback_CentersInsideReferenceScreenWorkArea()
    {
        var provider = CreateDualScreenProvider();
        var referenceBounds = new Rect(2200, 200, 280, 100);

        var corrected = WindowPlacementService.CenterOnCurrentScreenWorkArea(
            new Size(420, 220),
            referenceBounds,
            provider);

        Assert.Equal(2670, corrected.Left, 3);
        Assert.Equal(410, corrected.Top, 3);
        Assert.True(corrected.Left >= 1920);
        Assert.True(corrected.Right <= 3840);
        Assert.True(corrected.Bottom <= 1040);
    }

    private static FakeScreenWorkAreaProvider CreateDualScreenProvider()
    {
        return new FakeScreenWorkAreaProvider(
            new ScreenWorkArea(new Rect(0, 0, 1920, 1080), new Rect(0, 0, 1920, 1040)),
            new ScreenWorkArea(new Rect(1920, 0, 1920, 1080), new Rect(1920, 0, 1920, 1040)));
    }

    private sealed class FakeScreenWorkAreaProvider(params ScreenWorkArea[] screens) : IScreenWorkAreaProvider
    {
        private readonly IReadOnlyList<ScreenWorkArea> _screens = screens;

        public IReadOnlyList<ScreenWorkArea> GetScreens() => _screens;

        public ScreenWorkArea GetNearestScreen(Rect windowBounds)
        {
            var intersecting = _screens.FirstOrDefault(screen => screen.Bounds.IntersectsWith(windowBounds));
            if (intersecting != default)
            {
                return intersecting;
            }

            var center = new Point(windowBounds.Left + (windowBounds.Width / 2d), windowBounds.Top + (windowBounds.Height / 2d));
            return _screens
                .OrderBy(screen => DistanceToRect(center, screen.Bounds))
                .First();
        }

        private static double DistanceToRect(Point point, Rect rect)
        {
            var dx = point.X < rect.Left
                ? rect.Left - point.X
                : point.X > rect.Right
                    ? point.X - rect.Right
                    : 0d;

            var dy = point.Y < rect.Top
                ? rect.Top - point.Y
                : point.Y > rect.Bottom
                    ? point.Y - rect.Bottom
                    : 0d;

            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
