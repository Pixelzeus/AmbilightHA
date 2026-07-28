using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AmbilightHA.Core.HomeAssistant;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Core.Throttling;

public sealed record LightUpdateCommand(string EntityId, RgbColor Color, int Brightness, float TransitionSeconds);

public sealed class LightRateLimiter : IDisposable
{
    private readonly HaWebSocketClient _wsClient;
    private readonly ConcurrentDictionary<string, Channel<LightUpdateCommand>> _entityChannels = new();
    private readonly CancellationTokenSource _cts = new();

    public int MaxUpdatesPerSecond { get; set; } = 8;

    public LightRateLimiter(HaWebSocketClient wsClient)
    {
        _wsClient = wsClient;
    }

    public void QueueUpdate(string entityId, RgbColor color, int brightness, float transitionSeconds)
    {
        var channel = _entityChannels.GetOrAdd(entityId, id =>
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

        channel.Writer.TryWrite(new LightUpdateCommand(entityId, color, brightness, transitionSeconds));
    }

    private async Task ProcessEntityQueueAsync(string entityId, ChannelReader<LightUpdateCommand> reader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (await reader.WaitToReadAsync(ct))
                {
                    if (reader.TryRead(out var cmd))
                    {
                        if (_wsClient.IsConnected)
                        {
                            await _wsClient.SendLightColorAsync(cmd.EntityId, cmd.Color, cmd.Brightness, cmd.TransitionSeconds, ct);
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
                System.Diagnostics.Debug.WriteLine($"Erreur RateLimiter sur {entityId}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _entityChannels.Clear();
    }
}
