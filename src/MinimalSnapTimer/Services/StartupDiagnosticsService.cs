using System.IO;
using System.Reflection;
using System.Text;

namespace MinimalSnapTimer.Services;

public sealed class StartupDiagnosticsService
{
    private readonly string _logDirectory;

    public StartupDiagnosticsService(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalSnapTimer",
            "logs");
    }

    public string LogDirectory => _logDirectory;

    public string LogPath => Path.Combine(_logDirectory, "startup.log");

    public void Write(string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            RotateIfNeeded();

            using var writer = new StreamWriter(LogPath, append: true, Encoding.UTF8);
            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            if (exception is not null)
            {
                writer.WriteLine(exception);
            }
        }
        catch
        {
        }
    }

    public void WriteEnvironmentSnapshot()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        Write("===== 应用启动 =====");
        Write($"进程路径: {Environment.ProcessPath ?? "(unknown)"}");
        Write($"工作目录: {Environment.CurrentDirectory}");
        Write($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        Write($"版本: {assembly.GetName().Version}");
        Write($"框架: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Write($"操作系统: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
        {
            return;
        }

        const long maxBytes = 512 * 1024;
        var fileInfo = new FileInfo(LogPath);
        if (fileInfo.Length < maxBytes)
        {
            return;
        }

        var archivePath = Path.Combine(_logDirectory, $"startup-{DateTime.Now:yyyyMMddHHmmss}.log");
        File.Move(LogPath, archivePath, overwrite: true);

        var staleArchives = Directory.GetFiles(_logDirectory, "startup-*.log")
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .Skip(5)
            .ToArray();

        foreach (var staleArchive in staleArchives)
        {
            try
            {
                File.Delete(staleArchive);
            }
            catch
            {
            }
        }
    }
}
