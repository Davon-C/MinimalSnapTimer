using System.Drawing;

namespace MinimalSnapTimer.Services;

public interface IIconService
{
    Icon LoadTrayIcon(Action<string, Exception?>? logger = null);
}
