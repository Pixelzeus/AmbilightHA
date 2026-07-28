using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Core.HomeAssistant;

public sealed class HaWebSocketClient : IDisposable
{
    private ClientWebSocket? _client;
    private readonly Uri _haUri;
    private readonly string _token;
    private long _messageId = 1;
    private CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<long, TaskCompletionSource<string>> _pendingRequests = new();
    private Task? _receiveTask;
    private bool _isConnected;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private readonly byte[] _receiveBuffer = new byte[16384];

    public bool IsConnected => _isConnected && _client?.State == WebSocketState.Open;

    public event Action<string>? OnLogMessage;
    public event Action<bool>? OnConnectionStateChanged;

    public HaWebSocketClient(string baseUrl, string token)
    {
        string wsUrl = baseUrl.TrimEnd('/')
            .Replace("http://", "ws://")
            .Replace("https://", "wss://");

        if (!wsUrl.EndsWith("/api/websocket"))
        {
            wsUrl += "/api/websocket";
        }

        _haUri = new Uri(wsUrl);
        _token = token;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            DisposeClient();
            _client = new ClientWebSocket();

            Log($"Connexion à {_haUri}...");
            await _client.ConnectAsync(_haUri, cancellationToken);

            // 1. Attente du message auth_required
            string firstMsg = await ReadSingleMessageAsync(cancellationToken);
            using var doc = JsonDocument.Parse(firstMsg);
            string type = doc.RootElement.GetProperty("type").GetString() ?? "";

            if (type != "auth_required")
            {
                Log($"Erreur Handshake HA: attendu 'auth_required', reçu '{type}'");
                return false;
            }

            // 2. Envoi du token d'accès
            var authPayload = new HaAuthMessage("auth", _token);
            string authJson = JsonSerializer.Serialize(authPayload);
            await SendTextMessageInternalAsync(authJson, cancellationToken);

            // 3. Vérification auth_ok
            string authResp = await ReadSingleMessageAsync(cancellationToken);
            using var respDoc = JsonDocument.Parse(authResp);
            string authResult = respDoc.RootElement.GetProperty("type").GetString() ?? "";

            if (authResult == "auth_ok")
            {
                _isConnected = true;
                _cts = new CancellationTokenSource();
                _receiveTask = Task.Run(() => BackgroundReceiveLoopAsync(_cts.Token));

                Log("Authentification Home Assistant réussie !");
                OnConnectionStateChanged?.Invoke(true);
                return true;
            }

            Log($"Échec authentification HA: {authResp}");
            return false;
        }
        catch (Exception ex)
        {
            Log($"Exception connexion WebSocket: {ex.Message}");
            _isConnected = false;
            OnConnectionStateChanged?.Invoke(false);
            return false;
        }
    }

    /// <summary>
    /// Boucle d'écoute arrière-plan qui draine le WebSocket et matche les réponses par ID.
    /// </summary>
    private async Task BackgroundReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _client?.State == WebSocketState.Open)
        {
            try
            {
                string msg = await ReadSingleMessageAsync(ct);
                if (string.IsNullOrEmpty(msg)) break;

                using var doc = JsonDocument.Parse(msg);
                if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.TryGetInt64(out long id))
                {
                    if (_pendingRequests.TryRemove(id, out var tcs))
                    {
                        tcs.TrySetResult(msg);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"[WS Receiver] Exception: {ex.Message}");
                await Task.Delay(50, ct);
            }
        }

        if (_isConnected)
        {
            _isConnected = false;
            OnConnectionStateChanged?.Invoke(false);
            Log("[WS Receiver] Déconnexion détectée.");
        }
    }

    /// <summary>
    /// Envoie une requête nécessitant une réponse JSON-RPC identifiée par son ID.
    /// </summary>
    private async Task<string> SendRequestAndWaitAsync(string requestJson, long reqId, int timeoutMs = 3000, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[reqId] = tcs;

        await SendTextMessageInternalAsync(requestJson, ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        try
        {
            using (cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        catch (TaskCanceledException)
        {
            _pendingRequests.TryRemove(reqId, out _);
            return string.Empty;
        }
    }

    /// <summary>
    /// Capture l'état et la couleur d'origine des ampoules sélectionnées dans Home Assistant.
    /// </summary>
    public async Task<List<InitialLightState>> FetchInitialLightStatesAsync(IEnumerable<string> targetEntityIds, CancellationToken ct = default)
    {
        var resultStates = new List<InitialLightState>();
        if (!IsConnected || _client == null) return resultStates;

        try
        {
            long reqId = Interlocked.Increment(ref _messageId);
            var req = new HaGetStatesMessage(reqId);
            string json = JsonSerializer.Serialize(req);

            string responseJson = await SendRequestAndWaitAsync(json, reqId, timeoutMs: 3000, ct);
            if (string.IsNullOrEmpty(responseJson))
            {
                Log("[State Capture] Timeout ou réponse vide de HA pour l'état d'origine.");
                return resultStates;
            }

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("result", out var resultArr) && resultArr.ValueKind == JsonValueKind.Array)
            {
                var targetSet = new HashSet<string>(targetEntityIds, StringComparer.OrdinalIgnoreCase);

                foreach (var stateItem in resultArr.EnumerateArray())
                {
                    string entityId = stateItem.GetProperty("entity_id").GetString() ?? "";
                    if (targetSet.Contains(entityId))
                    {
                        string state = stateItem.GetProperty("state").GetString() ?? "off";
                        bool isOn = state.Equals("on", StringComparison.OrdinalIgnoreCase);

                        int[]? rgbColor = null;
                        int? brightness = null;
                        int? colorTemp = null;

                        if (stateItem.TryGetProperty("attributes", out var attrs))
                        {
                            if (attrs.TryGetProperty("rgb_color", out var rgbProp) && rgbProp.ValueKind == JsonValueKind.Array)
                            {
                                rgbColor = rgbProp.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                            }
                            if (attrs.TryGetProperty("brightness", out var brProp) && brProp.ValueKind == JsonValueKind.Number)
                            {
                                brightness = brProp.GetInt32();
                            }
                            if (attrs.TryGetProperty("color_temp", out var ctProp) && ctProp.ValueKind == JsonValueKind.Number)
                            {
                                colorTemp = ctProp.GetInt32();
                            }
                        }

                        resultStates.Add(new InitialLightState(entityId, isOn, rgbColor, brightness, colorTemp));
                        Log($"[State Capture] {entityId} -> State: {state}, Brightness: {brightness}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Erreur lors de la capture des états d'origine HA: {ex.Message}");
        }

        return resultStates;
    }

    /// <summary>
    /// Restaure les ampoules dans leur état et couleur d'origine.
    /// </summary>
    public async Task RestoreLightStatesAsync(IEnumerable<InitialLightState> initialStates, CancellationToken ct = default)
    {
        if (!IsConnected || _client == null) return;

        foreach (var state in initialStates)
        {
            try
            {
                long id = Interlocked.Increment(ref _messageId);

                if (!state.IsOn)
                {
                    var turnOffMsg = new
                    {
                        id = id,
                        type = "call_service",
                        domain = "light",
                        service = "turn_off",
                        target = new { entity_id = state.EntityId }
                    };
                    await SendTextMessageInternalAsync(JsonSerializer.Serialize(turnOffMsg), ct);
                    Log($"[State Restore] Éteinte: {state.EntityId}");
                }
                else
                {
                    var serviceData = new HaLightServiceData(
                        RgbColor: state.RgbColor,
                        Brightness: state.Brightness,
                        ColorTemp: state.ColorTemp,
                        Transition: 1.0f
                    );

                    var restoreMsg = new HaCallServiceMessage(
                        Id: id,
                        Type: "call_service",
                        Domain: "light",
                        Service: "turn_on",
                        Target: new HaServiceTarget(state.EntityId),
                        ServiceData: serviceData
                    );

                    await SendTextMessageInternalAsync(JsonSerializer.Serialize(restoreMsg), ct);
                    Log($"[State Restore] Allumée: {state.EntityId} (Luminosité: {state.Brightness})");
                }

                await Task.Delay(50, ct);
            }
            catch (Exception ex)
            {
                Log($"Erreur de restauration sur {state.EntityId}: {ex.Message}");
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task SendLightColorAsync(string entityId, RgbColor color, int brightness, float transitionSeconds, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _client == null) return;

        long id = Interlocked.Increment(ref _messageId);

        var serviceMsg = new HaCallServiceMessage(
            Id: id,
            Type: "call_service",
            Domain: "light",
            Service: "turn_on",
            Target: new HaServiceTarget(entityId),
            ServiceData: new HaLightServiceData(
                RgbColor: new int[] { color.R, color.G, color.B },
                Brightness: Math.Clamp(brightness, 1, 255),
                Transition: transitionSeconds
            )
        );

        string json = JsonSerializer.Serialize(serviceMsg, JsonOptions);
        await SendTextMessageInternalAsync(json, cancellationToken);
    }

    private async Task SendTextMessageInternalAsync(string message, CancellationToken ct)
    {
        if (_client == null || _client.State != WebSocketState.Open) return;
        byte[] bytes = Encoding.UTF8.GetBytes(message);

        await _sendSemaphore.WaitAsync(ct);
        try
        {
            if (_client != null && _client.State == WebSocketState.Open)
            {
                await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
            }
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    private async Task<string> ReadSingleMessageAsync(CancellationToken ct)
    {
        if (_client == null) return string.Empty;
        var segment = new ArraySegment<byte>(_receiveBuffer);

        using var ms = new System.IO.MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await _client.ReceiveAsync(segment, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fermeture", ct);
                return string.Empty;
            }
            ms.Write(_receiveBuffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private void Log(string message) => OnLogMessage?.Invoke(message);

    private void DisposeClient()
    {
        try
        {
            _client?.Dispose();
            _client = null;
        }
        catch { }
    }

    public void Dispose()
    {
        _isConnected = false;
        _cts.Cancel();
        DisposeClient();
        _sendSemaphore.Dispose();
    }
}
