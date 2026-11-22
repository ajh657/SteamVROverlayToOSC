using Microsoft.Extensions.Logging;

namespace SteamVROverlayToOSC;

public sealed class BackgroundWorker : IDisposable
{
    private readonly OSCClient _oscClient;
    private readonly ILogger<BackgroundWorker>? _logger;
    private CancellationTokenSource? _cancellation;
    private Task? _task;

    public BackgroundWorker(OSCClient oscClient, ILogger<BackgroundWorker>? logger = null)
    {
        _oscClient = oscClient;
        _logger = logger;
    }

    public bool IsRunning => _task is { IsCompleted: false };

    public void Start()
    {
        if (IsRunning)
            return;

        _cancellation = new CancellationTokenSource();
        _task = RunAsync(_cancellation.Token);
    }

    public void Stop()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await _oscClient.SendAsync("/steamvr/heartbeat", Environment.TickCount);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "OSC background worker failed to send a heartbeat.");
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _oscClient.Dispose();
    }
}
