using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AmbilightHA.Core.HomeAssistant;
using AmbilightHA.Core.Models;

using AmbilightHA.Core.Wled;

namespace AmbilightHA.Core.Throttling;

public sealed record LightUpdateCommand(
    string QueueKey,
    string TargetId,
    LightDeviceType DeviceType,
    RgbColor Color,
    int Brightness,
    float TransitionSeconds
);

public sealed class LightRateLimiter : IDisposable, IAsyncDisposable
{
    private readonly HaWebSocketClient _wsClient;
    private readonly WledUdpClient _wledClient = new();
    private readonly ConcurrentDictionary<string, Channel<LightUpdateCommand>> _entityChannels = new();
    private readonly CancellationTokenSource _cts = new();

    public int MaxUpdatesPerSecond { get; set; } = 8;

    public LightRateLimiter(HaWebSocketClient wsClient)
    {
        _wsClient = wsClient;
    }

    public void QueueUpdate(string queueKey, string targetId, LightDeviceType deviceType, RgbColor color, int brightness, float transitionSeconds)
    {
        if (_cts.IsCancellationRequested || string.IsNullOrWhiteSpace(targetId)) return;

        var channel = _entityChannels.GetOrAdd(queueKey, id =>
        {
            var options = new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };
            var ch = Channel.CreateBounded<LightUpdateCommand>(options);

            _ = Task.Run(() => ProcessEntityQueueAsync(id, ch.Reader, _cts.Token));
            return ch;
        });

        channel.Writer.TryWrite(new LightUpdateCommand(queueKey, targetId, deviceType, color, brightness, transitionSeconds));
    }

    private async Task ProcessEntityQueueAsync(string queueKey, ChannelReader<LightUpdateCommand> reader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (await reader.WaitToReadAsync(ct))
                {
                    if (reader.TryRead(out var cmd))
                    {
                        if (cmd.DeviceType == LightDeviceType.WledDirectUdp)
                        {
                            await _wledClient.SendColorAsync(cmd.TargetId, cmd.Color, cmd.Brightness);
                        }
                        else if (_wsClient.IsConnected)
                        {
                            await _wsClient.SendLightColorAsync(cmd.TargetId, cmd.Color, cmd.Brightness, cmd.TransitionSeconds, ct);
                        }

                        int delayMs = (int)(1000.0f / Math.Clamp(MaxUpdatesPerSecond, 1, 30));
                        await Task.Delay(delayMs, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur RateLimiter sur {queueKey}: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        foreach (var pair in _entityChannels)
        {
            pair.Value.Writer.TryComplete();
        }
        _entityChannels.Clear();
        _wledClient.Dispose();
        await Task.Yield();
    }

    public void Dispose()
    {
        _cts.Cancel();
        foreach (var pair in _entityChannels)
        {
            pair.Value.Writer.TryComplete();
        }
        _entityChannels.Clear();
        _wledClient.Dispose();
    }
}
