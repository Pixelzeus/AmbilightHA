using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmbilightHA.Core.Capture;
using AmbilightHA.Core.ColorAnalysis;
using AmbilightHA.Core.HomeAssistant;
using AmbilightHA.Core.Models;
using AmbilightHA.Core.Throttling;

namespace AmbilightHA.Core.Engine;

public record ZoneLightMapping(
    ScreenZone Zone,
    string EntityId,
    LightDeviceType DeviceType = LightDeviceType.HomeAssistant,
    string WledIpAddress = ""
);

public sealed class AmbilightOrchestrator : IDisposable, IAsyncDisposable
{
    private readonly DxgiScreenCapture _capture;
    private readonly ColorProcessor _processor;
    private readonly ColorSmoother _smoother;
    private HaWebSocketClient? _wsClient;
    private LightRateLimiter? _rateLimiter;
    private List<InitialLightState> _initialLightStates = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _isRunning;

    // Configuration dynamique
    public List<ZoneLightMapping> Mappings { get; } = new();
    public ColorAnalysisMode Mode { get; set; } = ColorAnalysisMode.VibrantAccent;
    public float AccentWeightExponent { get; set; } = 2.0f;
    public float Saturation { get; set; } = 1.3f;
    public float Brightness { get; set; } = 1.0f;
    public float Gamma { get; set; } = 1.0f;
    public byte MinBrightness { get; set; } = 0;
    public float TransitionSeconds { get; set; } = 0.2f;
    public int TargetFps { get; set; } = 30;
    public int RateLimitPerBulb { get; set; } = 8;
    public int DisplayIndex { get; set; } = 0;
    public bool RestoreLightsOnStop { get; set; } = true;

    public bool IsRunning => _isRunning;

    public event Action<Dictionary<ZoneType, RgbColor>>? OnFrameProcessed;
    public event Action<string>? OnLog;

    public AmbilightOrchestrator()
    {
        _capture = new DxgiScreenCapture();
        _processor = new ColorProcessor();
        _smoother = new ColorSmoother();
    }

    public async Task StartAsync(string haUrl, string haToken)
    {
        if (_isRunning) return;

        Log("Démarrage du moteur Ambilight...");
        _smoother.Reset();

        _wsClient = new HaWebSocketClient(haUrl, haToken);
        _wsClient.OnLogMessage += Log;

        bool connected = await _wsClient.ConnectAsync();
        if (!connected)
        {
            Log("Impossible de se connecter à Home Assistant. Démarrage annulé.");
            _wsClient.Dispose();
            _wsClient = null;
            return;
        }

        // Capture des états d'origine des ampoules Home Assistant avant de démarrer la synchronisation
        var targetEntityIds = Mappings
            .Where(m => m.DeviceType != LightDeviceType.WledDirectUdp && !string.IsNullOrWhiteSpace(m.EntityId))
            .Select(m => m.EntityId)
            .Distinct();

        Log("Capture de la couleur et luminosité d'origine des ampoules HA...");
        _initialLightStates = await _wsClient.FetchInitialLightStatesAsync(targetEntityIds);

        _rateLimiter = new LightRateLimiter(_wsClient)
        {
            MaxUpdatesPerSecond = RateLimitPerBulb
        };

        _capture.Initialize();

        _cts = new CancellationTokenSource();
        _isRunning = true;

        _loopTask = Task.Run(() => CaptureAndProcessLoopAsync(_cts.Token));
        Log($"Boucle de capture Ambilight démarrée en Mode {Mode}.");
    }

    private async Task CaptureAndProcessLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                int frameDelayMs = (int)(1000.0f / Math.Clamp(TargetFps, 1, 60));

                List<ZoneLightMapping> currentMappings;
                lock (Mappings)
                {
                    currentMappings = Mappings.ToList();
                }

                var activeZones = new List<ScreenZone>();
                foreach (var m in currentMappings)
                {
                    if (!activeZones.Contains(m.Zone))
                        activeZones.Add(m.Zone);
                }

                if (activeZones.Count == 0)
                {
                    activeZones = ScreenZone.CreateDefaultZones();
                }

                bool captured = _capture.CaptureFrame((ptr, w, h, stride) =>
                {
                    var colors = _processor.ProcessZones(
                        ptr, w, h, stride,
                        activeZones,
                        step: 16,
                        saturation: Saturation,
                        brightness: Brightness,
                        gamma: Gamma,
                        minBrightness: MinBrightness,
                        mode: Mode,
                        accentWeightExponent: AccentWeightExponent
                    );

                    OnFrameProcessed?.Invoke(colors);

                    foreach (var mapping in currentMappings)
                    {
                        if (colors.TryGetValue(mapping.Zone.Type, out var rawColor))
                        {
                            string targetId = mapping.DeviceType == LightDeviceType.WledDirectUdp ? mapping.WledIpAddress : mapping.EntityId;
                            if (!string.IsNullOrWhiteSpace(targetId))
                            {
                                int targetBrightness = rawColor.CalculateBrightnessLevel(Brightness, Gamma, MinBrightness);
                                string queueKey = $"{mapping.DeviceType}_{targetId}";
                                var smoothedColor = _smoother.Smooth(queueKey, rawColor, alpha: 0.35f);
                                _rateLimiter?.QueueUpdate(queueKey, targetId, mapping.DeviceType, smoothedColor, targetBrightness, TransitionSeconds);
                            }
                        }
                    }
                }, timeoutMs: 15);

                await Task.Delay(captured ? frameDelayMs : 5, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Erreur dans la boucle Ambilight: {ex.Message}");
                await Task.Delay(100, ct);
            }
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        Log("Arrêt du moteur Ambilight...");
        _isRunning = false;

        _cts?.Cancel();
        if (_loopTask != null)
        {
            try { await _loopTask; } catch { }
        }

        _capture.Dispose();
        if (_rateLimiter != null)
        {
            await _rateLimiter.DisposeAsync();
            _rateLimiter = null;
        }

        if (RestoreLightsOnStop && _wsClient != null && _initialLightStates.Count > 0)
        {
            Log("Restauration des ampoules dans leur état et couleur d'origine...");
            await _wsClient.RestoreLightStatesAsync(_initialLightStates);
        }

        _wsClient?.Dispose();
        _wsClient = null;

        Log("Moteur Ambilight arrêté et éclairage restauré.");
    }

    private void Log(string message) => OnLog?.Invoke(message);

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public void Dispose()
    {
        try
        {
            Task.Run(StopAsync).GetAwaiter().GetResult();
        }
        catch { }
    }
}
