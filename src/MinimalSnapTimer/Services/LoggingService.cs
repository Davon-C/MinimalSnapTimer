using System.IO;
using System.Text;

namespace MinimalSnapTimer.Services;

public sealed class LoggingService
{
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
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            if (exception is not null)
            {
                line = $"{line}{Environment.NewLine}{exception}";
            }

            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
        }
    }
}
