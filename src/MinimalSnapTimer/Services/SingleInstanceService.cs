using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private Mutex? _mutex;
    private bool _ownsMutex;

    public SingleInstanceService(string appId)
    {
        _mutexName = $"Local\\{appId}";
        _pipeName = $"{appId}.pipe";
    }

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, _mutexName, out var createdNew);
        _ownsMutex = createdNew;
        return createdNew;
    }

    public async Task StartListeningAsync(Func<string[], Task> handler, CancellationToken cancellationToken)
    {
        if (!_ownsMutex)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(server, Encoding.UTF8);
                var payload = await reader.ReadToEndAsync();
                var command = Deserialize(payload);
                await handler(command.Args);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(250, cancellationToken);
            }
        }
    }

    public async Task<bool> ForwardToPrimaryAsync(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            await client.ConnectAsync(1500);
            using var writer = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            await writer.WriteAsync(Serialize(args));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Serialize(string[] args)
    {
        return JsonSerializer.Serialize(new SingleInstanceCommand { Args = args });
    }

    public static SingleInstanceCommand Deserialize(string payload)
    {
        return JsonSerializer.Deserialize<SingleInstanceCommand>(payload) ?? new SingleInstanceCommand();
    }

    public void Dispose()
    {
        if (_ownsMutex)
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }

            _ownsMutex = false;
        }

        _mutex?.Dispose();
    }
}
