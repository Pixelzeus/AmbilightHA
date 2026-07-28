namespace AmbilightHA.Core.Models;

public enum LightDeviceType
{
    /// <summary>
    /// Ampoule standard Home Assistant via WebSocket (ex: light.living_room).
    /// </summary>
    HomeAssistant,

    /// <summary>
    /// Équipement WLED contrôlé via entité Home Assistant (ex: light.wled_strip).
    /// </summary>
    WledHA,

    /// <summary>
    /// Synchronisation UDP temps réel ultra-rapide (<1ms) directement vers l'adresse IP de la carte WLED (Port 21324).
    /// </summary>
    WledDirectUdp
}
