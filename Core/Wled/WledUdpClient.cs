using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AmbilightHA.Core.Models;

namespace AmbilightHA.Core.Wled;

/// <summary>
/// Client UDP ultra-rapide (<1ms) pour transmettre la couleur en temps réel directement à une carte WLED via le protocole DRGB (Port 21324).
/// </summary>
public sealed class WledUdpClient : IDisposable
{
    private const int WledUdpPort = 21324;
    private readonly UdpClient _udpClient = new();
    private readonly ConcurrentDictionary<string, IPEndPoint> _endPointCache = new();

    public async Task SendColorAsync(string ipAddress, RgbColor color, int brightness)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return;

        try
        {
            if (!_endPointCache.TryGetValue(ipAddress, out var endPoint))
            {
                if (IPAddress.TryParse(ipAddress.Trim(), out var parsedIp))
                {
                    endPoint = new IPEndPoint(parsedIp, WledUdpPort);
                    _endPointCache[ipAddress] = endPoint;
                }
                else
                {
                    return;
                }
            }

            float scale = Math.Clamp(brightness, 1, 255) / 255.0f;
            byte r = (byte)Math.Clamp(color.R * scale, 0, 255);
            byte g = (byte)Math.Clamp(color.G * scale, 0, 255);
            byte b = (byte)Math.Clamp(color.B * scale, 0, 255);

            // Protocole WLED DRGB Realtime : Header [0x02, Timeout (sec)], suivis des composantes RVB
            byte[] packet = new byte[]
            {
                0x02, // Protocole DRGB Realtime
                0x02, // Timeout 2 secondes avant de revenir au pattern WLED d'origine
                r,
                g,
                b
            };

            await _udpClient.SendAsync(packet, packet.Length, endPoint);
        }
        catch { }
    }

    public void Dispose()
    {
        try
        {
            _udpClient.Dispose();
        }
        catch { }
    }
}
