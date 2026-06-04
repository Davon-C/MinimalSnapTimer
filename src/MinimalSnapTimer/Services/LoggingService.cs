using System.IO;
using System.Text;

namespace MinimalSnapTimer.Services;

public sealed class LoggingService
{
    private const long MaxBytesPerLogFile = 512 * 1024;
    private const int MaxLogFilesToKeep = 30;
    private readonly string _logDirectory;

    public LoggingService(string? baseDirectory = null)
    {
        var root = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinimalSnapTimer",
            "logs");
        _logDirectory = root;
    }

    public void Write(string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            var path = Path.Combine(_logDirectory, $"{DateTime.Now:yyyyMMdd}.log");
            RotateIfNeeded(path);
            TrimStaleLogs();
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (exception is not null)
            {
                line = $"{line}{Environment.NewLine}{exception}";
            }

            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            TrimStaleLogs();
        }
        catch
        {
        }
    }

    private void RotateIfNeeded(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length < MaxBytesPerLogFile)
        {
            return;
        }

        var archivePath = Path.Combine(
            _logDirectory,
            $"{Path.GetFileNameWithoutExtension(path)}-{DateTime.Now:HHmmss}.log");

        File.Move(path, archivePath, overwrite: true);
    }

    private void TrimStaleLogs()
    {
        var staleLogs = Directory.GetFiles(_logDirectory, "*.log")
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .Skip(MaxLogFilesToKeep)
            .ToArray();

        foreach (var staleLog in staleLogs)
        {
            try
            {
                File.Delete(staleLog);
            }
            catch
            {
            }
        }
    }
}
