using System.Drawing;
using System.IO;
using System.Windows;

namespace MinimalSnapTimer.Services;

public sealed class IconService : IIconService
{
    private static readonly Uri[] ResourceUris =
    [
        new("pack://application:,,,/MinimalSnapTimer;component/Assets/AppIcon.ico", UriKind.Absolute),
        new("pack://application:,,,/Assets/AppIcon.ico", UriKind.Absolute)
    ];

    private readonly Func<string?> _processPathProvider;
    private readonly Func<Stream?> _resourceStreamFactory;

    public IconService(Func<string?>? processPathProvider = null, Func<Stream?>? resourceStreamFactory = null)
    {
        _processPathProvider = processPathProvider ?? (() => Environment.ProcessPath);
        _resourceStreamFactory = resourceStreamFactory ?? OpenDefaultResourceStream;
    }

    public Icon LoadTrayIcon(Action<string, Exception?>? logger = null)
    {
        if (TryLoadFromExecutable(out var executableIcon, out var executableException))
        {
            logger?.Invoke("托盘图标已从当前可执行文件提取。", null);
            return executableIcon;
        }

        if (executableException is not null)
        {
            logger?.Invoke("从当前可执行文件提取托盘图标失败。", executableException);
        }

        if (TryLoadFromResource(out var resourceIcon, out var resourceException))
        {
            logger?.Invoke("托盘图标已从内置资源加载。", null);
            return resourceIcon;
        }

        if (resourceException is not null)
        {
            logger?.Invoke("从内置资源加载托盘图标失败。", resourceException);
        }

        logger?.Invoke("托盘图标已回退到系统默认应用图标。", null);
        return (Icon)SystemIcons.Application.Clone();
    }

    private bool TryLoadFromExecutable(out Icon icon, out Exception? exception)
    {
        icon = null!;
        exception = null;

        try
        {
            var processPath = _processPathProvider();
            if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
            {
                return false;
            }

            var fileName = Path.GetFileName(processPath);
            if (string.Equals(fileName, "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var extracted = Icon.ExtractAssociatedIcon(processPath);
            if (extracted is null)
            {
                return false;
            }

            icon = (Icon)extracted.Clone();
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    private bool TryLoadFromResource(out Icon icon, out Exception? exception)
    {
        icon = null!;
        exception = null;

        try
        {
            using var stream = _resourceStreamFactory();
            if (stream is null)
            {
                return false;
            }

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;
            using var resourceIcon = new Icon(buffer);
            icon = (Icon)resourceIcon.Clone();
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    private static Stream? OpenDefaultResourceStream()
    {
        foreach (var uri in ResourceUris)
        {
            try
            {
                var resource = System.Windows.Application.GetResourceStream(uri);
                if (resource?.Stream is not null)
                {
                    return resource.Stream;
                }
            }
            catch
            {
            }
        }

        return null;
    }
}
